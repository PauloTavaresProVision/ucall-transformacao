#!/usr/bin/env bash
# Actualiza a aplicação no servidor: backup, git pull, instalar.sh e verificação.
# Uso:
#   bash actualizar.sh                         # actualização normal
#   bash actualizar.sh --url https://dominio   # define o URL público (links nos emails)
#   bash actualizar.sh --limpar                # aplica também sql/002-review-cleanup.sql
#   bash actualizar.sh --sem-backup            # não recomendado
set -Eeuo pipefail
umask 077

main() {
  cd "$(dirname "${BASH_SOURCE[0]}")"
  fail() { printf 'ERRO: %s\n' "$*" >&2; exit 1; }
  step() { printf '\n==> %s\n' "$*"; }

  local url="" clean=false backup=true
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --url) [[ $# -ge 2 ]] || fail "--url precisa de um endereço."; url="$2"; shift 2;;
      --limpar) clean=true; shift;;
      --sem-backup) backup=false; shift;;
      *) fail "Opção desconhecida: $1";;
    esac
  done
  [[ -z "$url" || "$url" == https://* ]] || fail "O URL público tem de começar por https://"

  command -v docker >/dev/null || fail "É necessário ter Docker instalado."
  docker compose version >/dev/null 2>&1 || fail "É necessário Docker Compose v2."

  # Instalação actual (online, ou a anterior configurada à mão).
  local env_file mode
  local -a args
  if [[ -f .env.online ]]; then
    env_file=.env.online; mode=online
    args=(--env-file .env.online -f compose.online.yaml)
  elif [[ -f .env.contabo && -f compose.contabo.yaml ]]; then
    env_file=.env.contabo; mode=contabo
    args=(--env-file .env.contabo -f compose.contabo.yaml)
    [[ ! -f compose.proxy.yaml ]] || args+=(-f compose.proxy.yaml)
  else
    fail "Não encontrei uma instalação nesta pasta. Para instalar de raiz: bash instalar.sh"
  fi
  printf 'Instalação encontrada: %s\n' "$env_file"

  # PostgreSQL próprio (compose.online.yaml) ou um container externo indicado em PG_HOST.
  local services has_pg=false
  services=$(docker compose "${args[@]}" config --services 2>/dev/null || true)
  ! grep -Fxq postgres <<< "$services" || has_pg=true

  local pg_db
  pg_db=""
  env_get() { sed -n "s/^$1=//p" "$env_file" | tail -n1 | tr -d '\r'; }

  if [[ "$backup" == true ]]; then
    step "Backup da base de dados e dos ficheiros/chaves"
    local stamp dir
    stamp=$(date +%Y%m%d-%H%M%S)
    dir="backups/$stamp"
    mkdir -p "$dir"

    if [[ "$has_pg" == true ]]; then
      docker compose "${args[@]}" exec -T postgres \
        pg_dump -U ucall_transformation ucall_transformation > "$dir/base.sql"
    else
      local pg_host pg_user pg_db pg_pass
      pg_host=$(env_get PG_HOST); pg_user=$(env_get PG_USER)
      pg_db=$(env_get PG_DATABASE); pg_pass=$(env_get PG_PASSWORD)
      pg_db=${pg_db:-ucall_transformation}
      [[ -n "$pg_host" && -n "$pg_user" ]] || fail "Faltam PG_HOST/PG_USER em $env_file."
      docker exec -i -e PGPASSWORD="$pg_pass" "$pg_host" \
        pg_dump -h 127.0.0.1 -U "$pg_user" "$pg_db" > "$dir/base.sql"
    fi
    [[ -s "$dir/base.sql" ]] || fail "O backup da base ficou vazio. Nada foi actualizado."

    local volume="" id
    for id in $(docker compose "${args[@]}" ps -q 2>/dev/null); do
      volume=$(docker inspect -f '{{range .Mounts}}{{if eq .Destination "/app/data"}}{{.Name}}{{end}}{{end}}' "$id")
      [[ -z "$volume" ]] || break
    done
    if [[ -n "$volume" ]]; then
      docker run --rm -v "$volume":/data:ro -v "$PWD/$dir":/backup alpine \
        tar czf /backup/app_data.tgz -C /data .
      [[ -s "$dir/app_data.tgz" ]] || fail "O backup dos ficheiros ficou vazio. Nada foi actualizado."
    else
      fail "Não encontrei o volume /app/data da aplicação (está a correr?). Nada foi actualizado."
    fi
    printf 'Backup guardado em %s:\n' "$dir"
    ls -lh "$dir"
  fi

  step "URL público"
  local url_key=UCALL_PUBLIC_URL
  [[ "$mode" == online ]] || url_key=PUBLIC_URL
  if [[ -n "$url" ]]; then
    if grep -q "^$url_key=" "$env_file"; then
      sed -i "s#^$url_key=.*#$url_key=$url#" "$env_file"
    else
      printf '%s=%s\n' "$url_key" "$url" >> "$env_file"
    fi
  fi
  local current
  current=$(env_get "$url_key")
  if [[ -n "$current" ]]; then
    printf '%s=%s\n' "$url_key" "$current"
  else
    printf 'AVISO: %s não está definido. Os emails não terão links correctos.\n' "$url_key"
    printf '       Corre de novo com: bash actualizar.sh --url https://o-teu-dominio\n'
  fi

  step "Buscar a versão mais recente (main)"
  git fetch --quiet origin
  git checkout --quiet main
  git pull --ff-only
  git log --oneline -1

  step "Construir e reiniciar a aplicação"
  bash instalar.sh

  if [[ "$mode" == online ]] && command -v curl >/dev/null; then
    step "Verificação"
    curl -fsS http://127.0.0.1:7766/api/health && printf '\nA aplicação está a responder.\n'
  fi

  if [[ "$clean" == true ]]; then
    step "Limpeza dos registos de teste (sql/002-review-cleanup.sql)"
    if [[ "$has_pg" == true ]]; then
      docker compose "${args[@]}" exec -T postgres \
        psql -U ucall_transformation -d ucall_transformation -v ON_ERROR_STOP=1 < sql/002-review-cleanup.sql
    else
      docker exec -i -e PGPASSWORD="$(env_get PG_PASSWORD)" "$(env_get PG_HOST)" \
        psql -h 127.0.0.1 -U "$(env_get PG_USER)" -d "${pg_db:-ucall_transformation}" -v ON_ERROR_STOP=1 < sql/002-review-cleanup.sql
    fi
  fi

  step "Concluído"
  printf 'Se algo correr mal: docker logs --tail 80 ucall-transformation\n'
}

# Tudo dentro de main(): o git pull pode alterar este ficheiro enquanto corre.
main "$@"
