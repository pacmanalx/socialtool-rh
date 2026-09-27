#!/usr/bin/env bash
# loadandrun.sh — roda NO SERVIDOR. Sobe o container da imagem que o publishdocker.sh acabou
# de buildar aqui mesmo. docker run (restart unless-stopped), sem compose.
#
# Não há `docker load`: o build é nativo no servidor, então a imagem já está no daemon local.
set -euo pipefail
cd "$(dirname "$0")"

[ -f .env.deploy ] && set -a && . ./.env.deploy && set +a

DK="${DK:-docker}"
IMG="socialtool:latest"
NOME="${CONTAINER:-socialtool}"
PORTA="${PORTA:-5185}"
# Segredos (banco, JWT) e configuração vivem no SERVIDOR, legíveis só pelo dono (600) —
# nunca na imagem, que circula. Chaves no formato do ASP.NET: ConnectionStrings__DefaultConnection,
# Jwt__SecretKey, App__PublicUrl, Bootstrap__*, Email__* (modelo em .env.example).
ENVFILE="${ENVFILE:-/etc/socialtool/api.env}"
REDE="${REDE:-socialtool}"
# Caixa de e-mail interna (Mailpit): guarda as mensagens e NÃO entrega nada. É o SMTP padrão do
# app neste servidor (Email__Smtp__Host=socialtool-mailpit). MAILPIT=off para não subir.
MAILPIT="${MAILPIT:-on}"
MAILPIT_UI_PORTA="${MAILPIT_UI_PORTA:-8026}"

if [ ! -f "$ENVFILE" ]; then
  echo "ERRO: não achei $ENVFILE. Ele traz ConnectionStrings__DefaultConnection, Jwt__SecretKey e o Bootstrap." >&2
  exit 1
fi

# Rede própria para o app falar com o Mailpit pelo nome, sem publicar o SMTP no host.
$DK network inspect "$REDE" >/dev/null 2>&1 || $DK network create "$REDE" >/dev/null

if [ "$MAILPIT" = "on" ] && ! $DK ps --format '{{.Names}}' | grep -qx "${NOME}-mailpit"; then
  echo "==> Subindo a caixa de e-mail interna (${NOME}-mailpit)"
  $DK rm -f "${NOME}-mailpit" >/dev/null 2>&1 || true
  # Interface só no loopback: acesse com ssh -L 8025:127.0.0.1:${MAILPIT_UI_PORTA} <servidor>
  $DK run -d --name "${NOME}-mailpit" --restart unless-stopped --network "$REDE" \
    -p "127.0.0.1:${MAILPIT_UI_PORTA}:8025" axllent/mailpit:latest >/dev/null
fi

echo "==> [1/3] Removendo container anterior"
$DK rm -f "$NOME" 2>/dev/null || true

# A porta é publicada SÓ no loopback: quem entra de fora é o túnel/proxy, que fala com
# localhost. Publicar em 0.0.0.0 exporia o app na rede sem necessidade.
#
# O MySQL é o NATIVO do servidor, alcançado por host.docker.internal. Não há container de
# banco: backup, upgrade e monitoração seguem os do host.
echo "==> [2/3] Subindo container"
$DK run -d \
  --name "$NOME" \
  --restart unless-stopped \
  --network "$REDE" \
  --env-file "$ENVFILE" \
  -e ASPNETCORE_URLS="http://0.0.0.0:8080" \
  -e ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}" \
  --add-host host.docker.internal:host-gateway \
  -p "127.0.0.1:${PORTA}:8080" \
  "$IMG" >/dev/null

echo "==> [3/3] Aguardando responder"
# A primeira subida aplica as migrations e cria a organização e o primeiro administrador.
for _ in $(seq 1 30); do
  if curl -fsS -m3 -o /dev/null "http://127.0.0.1:${PORTA}/api/health" 2>/dev/null; then
    echo "==> No ar em 127.0.0.1:${PORTA} ($(cat REVISION 2>/dev/null || echo 'revisão desconhecida'))"
    exit 0
  fi
  sleep 2
done

echo "ERRO: o container subiu mas /api/health não respondeu. Últimas linhas:" >&2
$DK logs --tail 40 "$NOME" >&2
exit 1
