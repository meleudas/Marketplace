#!/usr/bin/env bash
set -euo pipefail

# Restore Postgres from gzip SQL dump.
# Usage: postgres-restore.sh /path/to/backup.sql.gz [--dry-run]

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <backup.sql.gz> [--dry-run]" >&2
  exit 1
fi

FILE="$1"
DRY_RUN="${2:-}"

HOST="${POSTGRES_HOST:-localhost}"
PORT="${POSTGRES_PORT:-5432}"
DB="${POSTGRES_DB:-marketplace}"
USER="${POSTGRES_USER:-postgres}"
PASSWORD="${POSTGRES_PASSWORD:-postgres}"

if [[ ! -f "${FILE}" ]]; then
  echo "Backup file not found: ${FILE}" >&2
  exit 1
fi

if [[ "${DRY_RUN}" == "--dry-run" ]]; then
  gzip -dc "${FILE}" | head -n 20
  echo "... dry-run only (first 20 lines shown)"
  exit 0
fi

export PGPASSWORD="${PASSWORD}"
gzip -dc "${FILE}" | psql -h "${HOST}" -p "${PORT}" -U "${USER}" -d "${DB}" -v ON_ERROR_STOP=1
echo "Restore completed from ${FILE}"
