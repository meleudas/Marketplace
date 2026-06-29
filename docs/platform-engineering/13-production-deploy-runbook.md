# 13 — Production deploy runbook

Операційний runbook для deploy backend Marketplace через `docker-compose.prod.yml`.

## Prerequisites

- Docker Engine + Compose v2
- Заповнений `backend/.env` (див. [12-production-secrets-policy.md](12-production-secrets-policy.md))
- Root `.env` (опційно): `POSTGRES_PASSWORD`, `NOVAPOSHTA_API_KEY`, `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `CLICKHOUSE_PASSWORD`
- Backup Postgres перед оновленням

## Pre-deploy checklist

```powershell
# 1. Валідація секретів
./backend/scripts/ci/validate-production-env.ps1 -EnvFile backend/.env

# 2. Локальний deploy smoke (опційно)
./backend/scripts/ci/deploy-smoke.sh

# 3. CI gate перед тегом
# - integration-full-main на push до main
# - backend-release workflow на tag v*
```

## Deploy (manual)

```bash
git fetch --tags
git checkout vX.Y.Z

# Перевірка конфігурації
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --project backend/src/Marketplace.API -- --validate-config-only

# Збірка та старт
docker compose -f docker-compose.prod.yml pull   # якщо images у registry
docker compose -f docker-compose.prod.yml up -d --build

# Smoke
curl -fsS http://localhost:8080/health
curl -fsS http://localhost:8080/health/ready
curl -fsS http://localhost:8080/health/liqpay/config
```

## Post-deploy verification

| Endpoint | Очікування |
|----------|------------|
| `GET /health` | 200, `status: ok` |
| `GET /health/ready` | 200, postgres `Healthy` |
| `GET /health/liqpay/config` | 200 (prod keys) |
| Grafana (profile observability) | dashboards platform-dlq |

## Shipping (Nova Poshta)

У Production за замовчуванням:

- `SHIPPING__ENABLED=true`
- `SHIPPING__NOVAPOSHTAENABLED=true`
- `NOVAPOSHTA_API_KEY` — обов'язковий у root `.env` або `backend/.env`

API **не стартує** без API key (fail-fast validator).

## Rollback

1. `docker compose -f docker-compose.prod.yml stop api frontend`
2. Deploy попереднього image tag: `marketplace-api:vPREV`
3. `Database__AutoMigrate=false` якщо міграція несумісна
4. Restore Postgres з backup якщо потрібно (див. нижче)
5. Перевірити `/health/ready`

## Postgres backup / restore

| Параметр | Значення |
|----------|----------|
| RPO | 24 год (daily backup) |
| RTO | 4 год (manual restore + smoke) |
| Retention | 7 daily + 4 weekly (оператор) |

### Backup

```bash
export POSTGRES_PASSWORD=...
export BACKUP_DIR=/var/backups/marketplace
./backend/scripts/ops/postgres-backup.sh
```

Windows:

```powershell
./backend/scripts/ops/postgres-backup.ps1 -Password $env:POSTGRES_PASSWORD
```

Optional compose sidecar:

```bash
docker compose -f docker-compose.prod.yml -f docker-compose.ops.yml --profile ops run --rm postgres-backup
```

### Restore (dry-run)

```bash
./backend/scripts/ops/postgres-restore.sh /var/backups/marketplace/marketplace-YYYYMMDD.sql.gz --dry-run
```

### Restore (production)

1. Stop API: `docker compose -f docker-compose.prod.yml stop api`
2. Restore: `./backend/scripts/ops/postgres-restore.sh <backup.sql.gz>`
3. Start API та перевірити `/health/ready`


## CI/CD

| Workflow | Тригер | Що робить |
|----------|--------|-----------|
| [backend-ci.yml](../../.github/workflows/backend-ci.yml) `integration-full-main` | push `main` | containers + E2E |
| [backend-release.yml](../../.github/workflows/backend-release.yml) | tag `v*` | integration-full + deploy-smoke + docker build |

**Branch protection (рекомендовано):** required check `integration-full-main`. Деталі: [16-github-branch-protection.md](16-github-branch-protection.md).

## Пов'язані документи

- [12-production-secrets-policy.md](12-production-secrets-policy.md)
- [10-staging-production-rollout.md](10-staging-production-rollout.md)
- [09-local-docker-compose-runbook.md](09-local-docker-compose-runbook.md)
- [reports/production-readiness/executive-production-readiness.md](../../reports/production-readiness/executive-production-readiness.md)
