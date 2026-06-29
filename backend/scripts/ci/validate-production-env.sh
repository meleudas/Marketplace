#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-backend/.env}"
REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$REPO_ROOT"

export ASPNETCORE_ENVIRONMENT=Production

if [[ -f "$ENV_FILE" ]]; then
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%%#*}"
    line="$(echo "$line" | xargs)"
    [[ -z "$line" ]] && continue
    name="${line%%=*}"
    value="${line#*=}"
    value="${value%\"}"
    value="${value#\"}"
    export "$name=$value"
  done < "$ENV_FILE"
fi

echo "Validating production configuration (env file: $ENV_FILE)..."
dotnet run --project backend/src/Marketplace.API/Marketplace.API.csproj -c Release -- --validate-config-only
echo "PASS: Production configuration validation succeeded."
