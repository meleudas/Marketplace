#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$REPO_ROOT"

COMPOSE_FILE="docker-compose.smoke.yml"
API_PORT="${API_PORT:-8080}"
MAX_WAIT_SECONDS="${MAX_WAIT_SECONDS:-180}"

cleanup() {
  docker compose -f "$COMPOSE_FILE" down -v --remove-orphans 2>/dev/null || true
}
trap cleanup EXIT

echo "==> Deploy smoke: building and starting postgres + api..."
docker compose -f "$COMPOSE_FILE" up -d --build

echo "==> Waiting for API (max ${MAX_WAIT_SECONDS}s)..."
deadline=$((SECONDS + MAX_WAIT_SECONDS))
until curl -fsS "http://localhost:${API_PORT}/health/ready" >/dev/null 2>&1; do
  if (( SECONDS >= deadline )); then
    echo "FAIL: timed out waiting for /health/ready"
    docker compose -f "$COMPOSE_FILE" logs api
    exit 1
  fi
  sleep 3
done

echo "==> Checking /health..."
curl -fsS "http://localhost:${API_PORT}/health" | grep -q '"status"' || {
  echo "FAIL: /health did not return ok"
  docker compose -f "$COMPOSE_FILE" logs api
  exit 1
}

echo "==> Checking /health/ready..."
curl -fsS "http://localhost:${API_PORT}/health/ready" | grep -q '"status"' || {
  echo "FAIL: /health/ready did not return ok"
  docker compose -f "$COMPOSE_FILE" logs api
  exit 1
}

echo "PASS: Deploy smoke succeeded (migrate + API + /health/ready)."
