#!/usr/bin/env bash
# Publica o SocialTool RH num servidor com Docker, via SSH. Roda na sua máquina, a partir do repositório.
#
#   deploy/publish.sh
#
# Configuração em deploy/.env.deploy (fora do git; modelo em deploy/.env.deploy.example).
# Publica um COMMIT, não a pasta de trabalho: só a branch main, sem alterações pendentes.
# O build acontece no próprio servidor (arquitetura nativa, sem emulação nem transferência de imagem).
set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -f deploy/.env.deploy ]; then
  echo "ERRO: falta deploy/.env.deploy. Copie de deploy/.env.deploy.example e preencha." >&2
  exit 1
fi
set -a && . deploy/.env.deploy && set +a
: "${DEPLOY_HOST:?defina DEPLOY_HOST}" "${SERVER_ENV_FILE:?defina SERVER_ENV_FILE}"
REMOTE_DIR="${REMOTE_DIR:-socialtool}"
PUBLISH_BRANCH="${PUBLISH_BRANCH:-main}"

branch="$(git rev-parse --abbrev-ref HEAD)"
if [ "$branch" != "$PUBLISH_BRANCH" ]; then
  echo "ERRO: só a branch ${PUBLISH_BRANCH} é publicada (você está em ${branch})." >&2
  exit 1
fi
if [ -n "$(git status --porcelain)" ]; then
  echo "ERRO: há alterações não commitadas. Publique só o que está commitado." >&2
  exit 1
fi
commit="$(git rev-parse --short HEAD)"

echo "==> [1/3] Enviando ${PUBLISH_BRANCH}@${commit} para ${DEPLOY_HOST}:${REMOTE_DIR}"
# Espelho limpo do commit: nada da pasta anterior sobra no build.
ssh "$DEPLOY_HOST" "rm -rf '${REMOTE_DIR}.new' && mkdir -p '${REMOTE_DIR}.new'"
git archive --format=tar HEAD | ssh "$DEPLOY_HOST" "tar -x -C '${REMOTE_DIR}.new'"
ssh "$DEPLOY_HOST" "echo '${commit}' > '${REMOTE_DIR}.new/REVISION' && rm -rf '${REMOTE_DIR}' && mv '${REMOTE_DIR}.new' '${REMOTE_DIR}'"

echo "==> [2/3] Build e subida no servidor"
ssh "$DEPLOY_HOST" "test -f ${SERVER_ENV_FILE} || { echo 'ERRO: ${SERVER_ENV_FILE} não existe no servidor (modelo: deploy/socialtool.env.example).' >&2; exit 1; }
  cd '${REMOTE_DIR}' && docker compose -p socialtool --env-file ${SERVER_ENV_FILE} -f deploy/docker-compose.yml up -d --build --remove-orphans"

echo "==> [3/3] Conferindo"
ssh "$DEPLOY_HOST" "port=\$(sed -n 's/^APP_PORT=//p' ${SERVER_ENV_FILE}); port=\${port:-5185}
  for i in \$(seq 1 45); do
    curl -fsS -m3 -o /dev/null \"http://127.0.0.1:\${port}/api/health\" 2>/dev/null && { echo \"    no ar em 127.0.0.1:\${port}\"; exit 0; }
    sleep 2
  done
  echo 'ERRO: o app não respondeu. Últimas linhas:' >&2
  docker compose -p socialtool logs --tail 40 app >&2; exit 1"

if [ -n "${PUBLIC_URL:-}" ]; then
  curl -fsS -m20 -o /dev/null -w "==> ${PUBLIC_URL} respondeu %{http_code} em %{time_total}s\n" "${PUBLIC_URL}/api/health" \
    || echo "AVISO: ${PUBLIC_URL} ainda não responde (proxy/túnel/DNS configurados?)." >&2
fi
echo "==> Publicado ${PUBLISH_BRANCH}@${commit} em ${DEPLOY_HOST}."
