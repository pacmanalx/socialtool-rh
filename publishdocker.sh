#!/usr/bin/env bash
# publishdocker.sh — rsync do código -> build NO DESTINO -> loadandrun. Roda na raiz do projeto,
# na sua máquina. Configuração em .env.deploy (modelo em .env.deploy.example; não versionar).
#
# POR QUE O BUILD É NO DESTINO
# Buildar aqui e mandar a imagem exige emular a arquitetura do servidor quando ela é diferente
# da sua (ex.: Mac ARM publicando em servidor x86_64), o que leva dezenas de minutos. Buildando
# no servidor o build é nativo e não há imagem de centenas de MB trafegando.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -f .env.deploy ]; then
  echo "ERRO: falta .env.deploy. Copie de .env.deploy.example e preencha." >&2
  exit 1
fi
set -a && . ./.env.deploy && set +a

REMOTO="${BUILD_DIR:-build/socialtool}"   # relativo ao home do usuário no destino
URL="${URL:-}"
BRANCH_PUBLICADA="${PUBLISH_BRANCH:-main}"

if [ -z "${DEPLOY_HOST:-}" ] || [[ "${DEPLOY_HOST}" == *"DEFINIR_HOST_AQUI"* ]]; then
  echo "==> [SKIP] DEPLOY_HOST ainda não foi definido. Preencha o .env.deploy." >&2
  exit 0
fi

# Só a branch principal vai para o servidor; branch de feature se testa localmente.
branch="$(git rev-parse --abbrev-ref HEAD)"
if [ "$branch" != "$BRANCH_PUBLICADA" ]; then
  echo "ERRO: só a branch ${BRANCH_PUBLICADA} é publicada (você está em ${branch})." >&2
  exit 1
fi
if [ -n "$(git status --porcelain)" ]; then
  echo "ERRO: há alterações não commitadas; o servidor receberia algo que não está no git." >&2
  exit 1
fi

echo "==> [1/4] Enviando código ($(git rev-parse --short HEAD)) para ${DEPLOY_HOST}"
# rsync não cria o diretório PAI do destino — só o último nível.
ssh "${DEPLOY_HOST}" "mkdir -p ${REMOTO}"
# --delete para o destino ser espelho: arquivo removido aqui some lá, sem resíduo de build
# anterior entrando na imagem. Configuração local (.env*, appsettings.Development.json) fica.
rsync -a --delete \
  --exclude 'node_modules/' --exclude 'bin/' --exclude 'obj/' --exclude 'dist/' \
  --exclude '.git/' --exclude '.DS_Store' --exclude 'docs/' \
  --exclude '.env' --exclude '.env.*' --exclude 'appsettings.Development.json' \
  ./ "${DEPLOY_HOST}:${REMOTO}/"
ssh "${DEPLOY_HOST}" "printf '%s\n' '$(git rev-parse --short HEAD)' > ${REMOTO}/REVISION"

echo "==> [2/4] Build NO DESTINO (arquitetura nativa, sem emulação)"
ssh "${DEPLOY_HOST}" "cd ${REMOTO} && ${DK:-docker} build -t socialtool:latest -f Dockerfile ."

echo "==> [3/4] Conferindo o bundle de produção"
# Um localhost aqui significa .env de desenvolvimento vazando para o build — o sintoma é
# "Failed to fetch" na máquina do usuário, não no servidor.
if ssh "${DEPLOY_HOST}" "${DK:-docker} run --rm --entrypoint sh socialtool:latest -c \
     'grep -lE \"localhost:[0-9]+\" /app/wwwroot/assets/*.js 2>/dev/null'" | grep -q .; then
  echo "ERRO: o bundle contém localhost. Confira frontend/.env* (use .env.development.local)." >&2
  exit 1
fi

echo "==> [4/4] Subindo o container"
# O .env.deploy vai junto para o loadandrun.sh ler PORTA, ENVFILE etc. do mesmo lugar.
scp -q .env.deploy "${DEPLOY_HOST}:${REMOTO}/.env.deploy"
ssh "${DEPLOY_HOST}" "chmod +x ${REMOTO}/loadandrun.sh && ${REMOTO}/loadandrun.sh"

if [ -n "$URL" ]; then
  echo
  curl -fsS -m20 -o /dev/null -w "==> $URL respondeu %{http_code} em %{time_total}s\n" "$URL/api/health" \
    || echo "AVISO: $URL ainda não responde (túnel/proxy e DNS configurados?)." >&2
fi
echo "==> Publicação concluída em ${DEPLOY_HOST}."
