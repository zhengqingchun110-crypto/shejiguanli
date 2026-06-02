#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/shejiguanli}"
BACKUP_DIR="${BACKUP_DIR:-$APP_ROOT/backups/postgres}"
LOG_DIR="${LOG_DIR:-$APP_ROOT/logs}"
STATUS_FILE="${STATUS_FILE:-$APP_ROOT/backups/last-backup-status.txt}"

cd "$APP_ROOT"
source .env

mkdir -p "$BACKUP_DIR" "$LOG_DIR" "$(dirname "$STATUS_FILE")"

timestamp="$(date +%Y%m%d-%H%M%S)"
backup_file="$BACKUP_DIR/postgres-$timestamp.sql.gz"
latest_file="$BACKUP_DIR/latest.sql.gz"

echo "[$(date '+%F %T')] starting postgres backup: $backup_file"
docker exec shejiguanli-postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip -9 > "$backup_file"

bytes="$(stat -c%s "$backup_file")"
if [ "$bytes" -lt 1024 ]; then
  echo "[$(date '+%F %T')] backup too small: ${bytes} bytes" >&2
  rm -f "$backup_file"
  exit 1
fi

cp -f "$backup_file" "$latest_file"

# Keep enough history for normal mistakes and delayed discovery.
find "$BACKUP_DIR" -name 'postgres-*.sql.gz' -type f -mtime +60 -delete

{
  echo "status=ok"
  echo "time=$(date '+%F %T %z')"
  echo "file=$backup_file"
  echo "bytes=$bytes"
} > "$STATUS_FILE"

echo "[$(date '+%F %T')] backup completed: ${bytes} bytes"
