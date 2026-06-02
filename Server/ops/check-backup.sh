#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/shejiguanli}"
BACKUP_DIR="${BACKUP_DIR:-$APP_ROOT/backups/postgres}"
MAX_AGE_HOURS="${MAX_AGE_HOURS:-30}"

latest="$(find "$BACKUP_DIR" -name 'postgres-*.sql.gz' -type f -printf '%T@ %s %p\n' 2>/dev/null | sort -nr | head -1 || true)"
if [ -z "$latest" ]; then
  echo "backup_status=missing"
  exit 1
fi

mtime="$(echo "$latest" | awk '{print $1}')"
bytes="$(echo "$latest" | awk '{print $2}')"
file="$(echo "$latest" | cut -d' ' -f3-)"
now="$(date +%s)"
age_seconds="$(awk -v now="$now" -v mtime="$mtime" 'BEGIN { printf "%d", now - mtime }')"
max_age_seconds="$((MAX_AGE_HOURS * 3600))"

if [ "$bytes" -lt 1024 ]; then
  echo "backup_status=too_small"
  echo "file=$file"
  echo "bytes=$bytes"
  exit 1
fi

if [ "$age_seconds" -gt "$max_age_seconds" ]; then
  echo "backup_status=too_old"
  echo "file=$file"
  echo "age_hours=$((age_seconds / 3600))"
  exit 1
fi

echo "backup_status=ok"
echo "file=$file"
echo "bytes=$bytes"
echo "age_hours=$((age_seconds / 3600))"
