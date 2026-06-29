#!/usr/bin/env bash
set -euo pipefail

# Postgres logical backup (gzip). Requires pg_dump in PATH or postgres client container.
# Env: POSTGRES_HOST, POSTGRES_PORT, POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD, BACKUP_DIR

HOST="${POSTGRES_HOST:-localhost}"
PORT="${POSTGRES_PORT:-5432}"
DB="${POSTGRES_DB:-marketplace}"
USER="${POSTGRES_USER:-postgres}"
PASSWORD="${POSTGRES_PASSWORD:-postgres}"
BACKUP_DIR="${BACKUP_DIR:-./backups/postgres}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="${BACKUP_DIR}/${DB}-${STAMP}.sql.gz"

mkdir -p "${BACKUP_DIR}"
export PGPASSWORD="${PASSWORD}"

pg_dump -h "${HOST}" -p "${PORT}" -U "${USER}" -d "${DB}" --no-owner --no-acl | gzip -9 > "${OUT_FILE}"

if [[ ! -s "${OUT_FILE}" ]]; then
  echo "Backup file is empty: ${OUT_FILE}" >&2
  exit 1
fi

echo "Backup written: ${OUT_FILE} ($(du -h "${OUT_FILE}" | awk '{print $1}'))"
