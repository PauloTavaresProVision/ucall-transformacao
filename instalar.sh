#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
cd "$(dirname "${BASH_SOURCE[0]}")"
fail() { printf '%s\n' "$*" >&2; exit 1; }
command -v docker >/dev/null || fail "É necessário ter Docker instalado."
docker info >/dev/null 2>&1 || fail "O Docker não está disponível."
docker compose version >/dev/null 2>&1 || fail "É necessário Docker Compose v2."
[[ -f Dockerfile && -f api/Ucall.Api.csproj && -f frontend/package.json ]] || fail "Falta o código da aplicação."

# Preservar uma instalação anterior que já tenha sido configurada.
if [[ -f .env.contabo && -f compose.contabo.yaml && ! -f .env.online ]]; then
  args=(--env-file .env.contabo -f compose.contabo.yaml)
  [[ ! -f compose.proxy.yaml ]] || args+=(-f compose.proxy.yaml)
  docker compose "${args[@]}" up -d --build
  printf '%s\n' "Instalação existente actualizada. Base, configuração e volumes preservados."
  exit 0
fi

[[ -f compose.online.yaml ]] || fail "Falta compose.online.yaml. Executa git pull."
if [[ ! -f .env.online ]]; then
  password=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
  token=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
  [[ ${#password} == 64 && ${#token} == 64 ]] || fail "Não foi possível gerar as credenciais."
  printf 'POSTGRES_PASSWORD=%s\nBOOTSTRAP_TOKEN=%s\n' "$password" "$token" > .env.online
  chmod 600 .env.online
fi

docker compose --env-file .env.online -f compose.online.yaml config --quiet
docker compose --env-file .env.online -f compose.online.yaml up -d --build

# Ligar a aplicação às redes do Nginx Proxy Manager, se existir.
while read -r name image; do
  case "$image" in
    *nginx-proxy-manager*)
      while read -r network; do
        case "$network" in ""|bridge|host|none) continue;; esac
        attached=$(docker inspect --format '{{range $name, $value := .NetworkSettings.Networks}}{{$name}}{{println}}{{end}}' ucall-transformation)
        if ! grep -Fxq "$network" <<< "$attached"; then
          docker network connect "$network" ucall-transformation || printf '%s\n' "Não foi possível ligar à rede $network. Usa o IP do servidor e porta 7766 no Nginx."
        fi
      done < <(docker inspect --format '{{range $name, $value := .NetworkSettings.Networks}}{{$name}}{{println}}{{end}}' "$name")
    ;;
  esac
done < <(docker ps --format '{{.Names}} {{.Image}}')

if command -v curl >/dev/null; then
  ready=false
  for attempt in {1..30}; do
    if curl -fsS http://127.0.0.1:7766/api/health >/dev/null; then ready=true; break; fi
    sleep 2
  done
  [[ "$ready" == true ]] || fail "A aplicação ainda não respondeu. Consulta: docker logs --tail 80 ucall-transformation"
fi
printf '%s\n' "Instalação concluída." \
  "Nginx Proxy Manager: HTTP / ucall-transformation / porta 8080." \
  "Alternativa: HTTP / IP do servidor / porta 7766." \
  "Activa HTTPS no Nginx para o login em produção." \
  "Código da primeira conta: consulta BOOTSTRAP_TOKEN em .env.online." \
  "Actualizações: git pull --ff-only && bash instalar.sh" \
  "O PostgreSQL e as credenciais foram criados automaticamente."
