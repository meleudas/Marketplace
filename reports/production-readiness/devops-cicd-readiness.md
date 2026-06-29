# DevOps & CI/CD Readiness

- Статус реалізації: `Implemented`
- Готовність: **84/100**
- Release status: `Ready`
- Confidence: `Medium`
- Дата аудиту: 2026-06-29

## Evidence

- CI workflow: [.github/workflows/backend-ci.yml](../../.github/workflows/backend-ci.yml) — 25+ jobs
- Prod deploy: [docker-compose.prod.yml](../../docker-compose.prod.yml)
- Dev/local: [docker-compose.yml](../../docker-compose.yml), [docker-compose.dev.yml](../../docker-compose.dev.yml)
- Observability: [docker-compose.observability.yml](../../docker-compose.observability.yml)
- Build: [evidence/build-release.log](./evidence/build-release.log) — Release PASS

## CI pipeline

| Job category | Runs on every PR | Примітка |
|--------------|------------------|----------|
| api-regression, contract-compat | Так | API + contract stability |
| security-regression, performance-baseline | Так | Security + perf smoke |
| 15× domain gates | Так | Per-domain unit+integration |
| unit-coverage-gate (8%) | Так | Низький поріг |
| architecture-gate | Так | Layering rules |
| observability-config-validate | Так | OTEL config |
| sonar-analysis | Якщо `SONAR_HOST_URL` | Опційно |
| integration-full | Label `integration-full` | Containers + E2E |

## Deploy

- `docker-compose.prod.yml`: compiled API + Next.js, `Database__AutoMigrate: true`, health depends_on.
- Secrets через `backend/.env` (не в git) — documented у compose header.
- OTEL disabled by default (`OTEL_ENABLED:-false`).

## Що готово

- Multi-job CI з artifact upload на failure.
- Observability stack окремим compose profile.
- Release build verified (0 errors).

## Blockers (P0)

- ~~Немає automated deploy pipeline~~ **CLOSED (MVP scope)** — [backend-release.yml](../../.github/workflows/backend-release.yml) + [13-production-deploy-runbook.md](../../docs/platform-engineering/13-production-deploy-runbook.md). Повний CD — P2.

## Near-term (P1)

- Migration/deploy smoke: `docker compose -f docker-compose.prod.yml up` + health check у CI (nightly).
- Rollback runbook (image tag pinning, DB migration rollback policy).
- Postgres backup schedule для prod volumes.
- Зробити SonarQube gate blocking (зараз optional).

## Optional (P2)

- Blue/green або canary deploy.
- Infrastructure as Code (Terraform) для managed services.

## Checklist

- [x] CI на pull_request + push main
- [x] Domain-focused test gates
- [x] Prod docker-compose з healthchecks
- [x] .env.example для конфігурації
- [x] Automated release workflow (tag `v*`)
- [x] Documented rollback + deploy runbook
- [x] Migration smoke у CI (release workflow)
