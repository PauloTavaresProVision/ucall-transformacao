#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
cd "$(dirname "${BASH_SOURCE[0]}")"

fail() { printf '%s\n' "$*" >&2; exit 1; }
command -v docker >/dev/null || fail "Docker não está instalado."
docker info >/dev/null 2>&1 || fail "O Docker não está a funcionar ou falta permissão."
docker compose version >/dev/null 2>&1 || fail "Falta Docker Compose v2."
test -f Dockerfile && test -f api/Ucall.Api.csproj && test -f frontend/package.json ||
  fail "O código da aplicação ainda não está neste repositório. Instalação interrompida."

if [[ ! -f .env.contabo ]]; then
  read -rp "Domínio da plataforma (sem https://): " domain
  [[ "$domain" =~ ^[A-Za-z0-9][A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]] || fail "Domínio inválido."
  docker ps --format 'table {{.Names}}\t{{.Image}}'
  read -rp "Nome do contentor PostgreSQL existente: " pg_container
  [[ "$pg_container" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || fail "Nome de contentor inválido."
  read -rp "Utilizador administrador desse PostgreSQL: " pg_admin
  [[ "$pg_admin" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || fail "Utilizador inválido."
  pg_network=$(docker inspect --format '{{range $name, $value := .NetworkSettings.Networks}}{{$name}}{{println}}{{end}}' "$pg_container" | head -n 1)
  [[ -n "$pg_network" && "$pg_network" != bridge && "$pg_network" != host && "$pg_network" != none ]] ||
    fail "O PostgreSQL precisa de uma rede Docker com nome e resolução de nomes."
  read -rp "Rede Docker do Nginx (Enter se Nginx corre no próprio servidor): " proxy_network
  if [[ -n "$proxy_network" ]]; then
    [[ "$proxy_network" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || fail "Nome de rede inválido."
    docker network inspect "$proxy_network" >/dev/null
  fi
  db=ucall_transformation
  role=ucall_transformation
  exists=$(docker exec "$pg_container" psql -U "$pg_admin" -d postgres -tAc     "SELECT (EXISTS(SELECT 1 FROM pg_database WHERE datname='ucall_transformation') OR EXISTS(SELECT 1 FROM pg_roles WHERE rolname='ucall_transformation'))::int")
  [[ "$exists" == 0 ]] || fail "A base ou utilizador ucall_transformation já existe. Não foi alterado. É necessário configurar a ligação existente."
  password=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
  token=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
  [[ ${#password} == 64 && ${#token} == 64 ]] || fail "Não foi possível gerar as credenciais."
  {
    printf "CREATE ROLE ucall_transformation LOGIN PASSWORD '%s';\n" "$password"
    printf "CREATE DATABASE ucall_transformation OWNER ucall_transformation;\n"
  } | docker exec -i "$pg_container" psql -v ON_ERROR_STOP=1 -U "$pg_admin" -d postgres >/dev/null
  cat > .env.contabo <<EOF
PUBLIC_URL=https://$domain
POSTGRES_NETWORK=$pg_network
POSTGRES_CONTAINER=$pg_container
POSTGRES_PASSWORD=$password
BOOTSTRAP_TOKEN=$token
EOF
  chmod 600 .env.contabo

  cat > compose.contabo.yaml <<'YAML'
name: ucall-transformation
services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: ucall-transformation
    restart: unless-stopped
    ports:
      - "127.0.0.1:8088:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      Database__Provider: Postgres
      Database__Initialize: "true"
      ConnectionStrings__Database: "Host=${POSTGRES_CONTAINER};Port=5432;Database=ucall_transformation;Username=ucall_transformation;Password=${POSTGRES_PASSWORD}"
      Storage__Root: /app/data
      Bootstrap__Token: ${BOOTSTRAP_TOKEN}
      App__PublicUrl: ${PUBLIC_URL}
    volumes:
      - app_data:/app/data
    networks:
      - postgres
networks:
  postgres:
    external: true
    name: ${POSTGRES_NETWORK}
volumes:
  app_data:
YAML
  if [[ -n "$proxy_network" && "$proxy_network" != "$pg_network" ]]; then
    cat > compose.proxy.yaml <<EOF
services:
  app:
    networks:
      - proxy
networks:
  proxy:
    external: true
    name: $proxy_network
EOF
  fi
fi

[[ -f compose.contabo.yaml ]] || fail "Falta compose.contabo.yaml."
args=(--env-file .env.contabo -f compose.contabo.yaml)
[[ ! -f compose.proxy.yaml ]] || args+=(-f compose.proxy.yaml)
docker compose "${args[@]}" config --quiet
docker compose "${args[@]}" up -d --build

ready=false
for attempt in {1..30}; do
  if command -v curl >/dev/null && curl -fsS http://127.0.0.1:8088/api/health >/dev/null; then
    ready=true
    break
  fi
  sleep 2
done
if [[ "$ready" != true ]]; then
  printf '%s\n' "Contentor criado. Verifica o estado antes de configurar o Nginx:"
  docker compose "${args[@]}" ps
  printf '%s\n' "Diagnóstico: docker logs --tail 80 ucall-transformation"
  exit 1
fi
printf '%s\n' "Aplicação disponível."   "Nginx no servidor: http://127.0.0.1:8088"   "Nginx em Docker na rede configurada: http://ucall-transformation:8080"   "Activa HTTPS no domínio escolhido."   "O token para criar a primeira conta está em .env.contabo (BOOTSTRAP_TOKEN)."   "Actualizações: git pull --ff-only && bash instalar.sh"   "Conserva .env.contabo e o volume app_data; contêm credenciais e chaves cifradas."
