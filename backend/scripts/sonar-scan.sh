#!/usr/bin/env bash
set -euo pipefail

SONAR_HOST_URL="${SONAR_HOST_URL:-}"
SONAR_TOKEN="${SONAR_TOKEN:-}"

if [[ -z "$SONAR_HOST_URL" || -z "$SONAR_TOKEN" ]]; then
  echo "Set SONAR_HOST_URL and SONAR_TOKEN." >&2
  exit 1
fi

cd "$(dirname "$0")/.."

dotnet tool install --global dotnet-sonarscanner --ignore-failed-sources || true

dotnet sonarscanner begin \
  /k:"marketplace-backend" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="**/Migrations/**,**/Contracts/**,**/test-results/**" \
  /d:sonar.cs.opencover.reportsPaths="tests/Marketplace.Tests/test-results/sonar-coverage/coverage.cobertura.xml"

dotnet build Marketplace.slnx -c Release
dotnet test tests/Marketplace.Tests/Marketplace.Tests.csproj -c Release --no-build \
  /p:CollectCoverage=true \
  /p:CoverletOutput=test-results/sonar-coverage/ \
  /p:CoverletOutputFormat=cobertura

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

# Після end проект/quality gate може з'явитись із затримкою,
# тож чекаємо короткими ретраями.
maxAttempts=30
delaySeconds=2
status=""

for attempt in $(seq 1 $maxAttempts); do
  resp=$(curl -s -u "${SONAR_TOKEN}:" \
    "${SONAR_HOST_URL}/api/qualitygates/project_status?projectKey=marketplace-backend" || true)

  status=$(echo "$resp" | jq -r '.projectStatus.status // empty' 2>/dev/null || true)
  if [[ "$status" == "OK" ]]; then
    echo "SonarQube Quality Gate: OK"
    exit 0
  fi
  if [[ "$status" == "NONE" ]]; then
    echo "Sonar scan uploaded successfully. Quality Gate is not configured yet (status=NONE)."
    exit 0
  fi

  msg=$(echo "$resp" | jq -r '.errors[0].msg // empty' 2>/dev/null || true)
  if [[ "$msg" == *"not found"* ]]; then
    sleep "$delaySeconds"
    continue
  fi

  if [[ -n "$status" ]]; then
    echo "SonarQube Quality Gate failed: $status" >&2
    exit 1
  fi
done

echo "SonarQube Quality Gate check failed (timeout waiting for project): ${status:-<empty>}" >&2
exit 1
