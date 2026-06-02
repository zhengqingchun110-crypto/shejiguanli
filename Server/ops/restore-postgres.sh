#!/usr/bin/env bash
set -euo pipefail

if [ "${1:-}" = "" ]; then
  echo "Usage: $0 /opt/shejiguanli/backups/postgres/postgres-YYYYMMDD-HHMMSS.sql.gz" >&2
  exit 2
fi

APP_ROOT="${APP_ROOT:-/opt/shejiguanli}"
backup_file="$1"

if [ ! -f "$backup_file" ]; then
  echo "Backup file not found: $backup_file" >&2
  exit 1
fi

cd "$APP_ROOT"
source .env

echo "This will replace database '$POSTGRES_DB' from:"
echo "$backup_file"
echo "Type RESTORE to continue:"
read -r confirmation
if [ "$confirmation" != "RESTORE" ]; then
  echo "Canceled."
  exit 0
fi

docker compose -f docker-compose.yml -f docker-compose.override.yml stop api || true
docker exec shejiguanli-postgres psql -U "$POSTGRES_USER" -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$POSTGRES_DB';"
docker exec shejiguanli-postgres dropdb -U "$POSTGRES_USER" --if-exists "$POSTGRES_DB"
docker exec shejiguanli-postgres createdb -U "$POSTGRES_USER" "$POSTGRES_DB"
gzip -dc "$backup_file" | docker exec -i shejiguanli-postgres psql -U "$POSTGRES_USER" "$POSTGRES_DB"
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d api

echo "Restore completed."
