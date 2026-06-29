# Infrastructure & Platform Services Readiness

- Статус реалізації: `Implemented`
- Готовність: **92/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- `dotnet test ...Integration.Containers --filter Layer=IntegrationContainers` → 32/32 pass ([evidence/test-containers.log](./evidence/test-containers.log))
- Health: [HealthCheckRegistrationExtensions.cs](../../backend/src/Marketplace.Infrastructure/Health/HealthCheckRegistrationExtensions.cs)
- Prod stack: [docker-compose.prod.yml](../../docker-compose.prod.yml)
- Container fixture: [MarketplaceContainersFixture.cs](../../backend/tests/Marketplace.Tests.Integration.Containers/Fixtures/MarketplaceContainersFixture.cs)

## Сервіси

| Сервіс | Роль | Graceful off | Health | Container test | Score |
|--------|------|--------------|--------|----------------|------:|
| PostgreSQL + EF | Primary store, migrations | — | `postgres` live+ready | Так (усі PG тести) | 92 |
| Redis | Cache, rate limiting | Config | `redis` ready | RedisRateLimitingContainersTests | 88 |
| Elasticsearch | Product search, similar | `Elasticsearch:Enabled` | `elasticsearch` ready | ElasticsearchSimilarProductsTests, AdminCatalogReindex | 85 |
| MinIO / S3 | Media, ML models | `Storage:Enabled` | `storage` ready | MinioStoragePostgresTests, RecommendationPipeline | 87 |
| ClickHouse | Analytics warehouse, ML training | `ClickHouse:Enabled` | `clickhouse` ready | ClickHouseWarehousePipelineContainersTests | 84 |
| Hangfire + Outbox | Async jobs, search index | — | `queue` (outbox) ready | OutboxDispatchPostgresTests, JobSmokePostgresTests | 90 |
| OpenTelemetry | Traces/metrics | `OpenTelemetry:Enabled` | — | observability-config-validate CI | 78 |

## Що готово

- Повний prod compose з postgres, redis, elasticsearch, minio, clickhouse, otel-collector.
- `Database__AutoMigrate: true` у prod compose для автоматичних міграцій при старті.
- ClickHouse schema з `allow_nullable_key` для CH 24.8 ([ClickHouseAnalyticsWarehouseWriter.cs](../../backend/src/Marketplace.Infrastructure/External/Analytics/ClickHouseAnalyticsWarehouseWriter.cs)).
- Outbox/inbox/idempotency з DLQ metrics і container coverage.

## Blockers (P0)

- ~~Немає migration smoke~~ **CLOSED** — `docker-compose.smoke.yml` + `deploy-smoke.sh` + CI job у `backend-release.yml`

## Near-term (P2)

- Документувати мінімальні RAM/CPU для ES (512m) + ClickHouse у prod runbook.

## Optional (P2)

- Окремий health для Hangfire dashboard reachability.
- Resource limits у `docker-compose.prod.yml` для всіх сервісів.

## Checklist

- [x] Postgres migrations існують і застосовуються (`InitializeDatabaseAsync`)
- [x] Optional deps вимикаються через `Enabled` flags
- [x] Ready/live health endpoints для критичних залежностей
- [x] Container suite проти реальної інфри (32 тести)
- [x] Migration smoke у CI (release workflow)
- [ ] Formalized backup/restore runbook
