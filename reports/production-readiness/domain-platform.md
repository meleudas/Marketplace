# Domain Report: Platform (Outbox, Idempotency, Jobs, Observability)

- Статус реалізації: `Implemented`
- Готовність: **91/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `platform-gate` (12% threshold)
- Container: OutboxDispatchPostgresTests, IntegrationRetryProcessorPostgresTests, RedisRateLimitingContainersTests, JobSmokePostgresTests

## Межі домену

- `backend/src/Marketplace.Application/Common`
- `backend/src/Marketplace.API/Extensions/HttpIdempotencyExtensions.cs`
- `backend/src/Marketplace.API/Controllers/AdminOutboxController.cs`
- `backend/src/Marketplace.Infrastructure/Jobs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/OutboxRepository.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/HttpIdempotencyStore.cs`

## Що вже готово

- Реалізовані і протестовані outbox/inbox/idempotency lifecycle сценарії: retry/backoff, dead-letter, requeue, request mismatch, TTL expiry.
- Розширено platform test contour: `jobs + outbox + inbox + idempotency + API + security + SQLite integration`.
- Додано окремий `Suite=Platform` та `platform-gate` у CI з coverage threshold `12%`.
- Підсилено observability для platform runtime: `outbox_dispatch_total`, `outbox_dispatch_errors_total`, `outbox_dead_letter_total`, `idempotency_begin_total`, `idempotency_conflicts_total`, `idempotency_replays_total`.
- Синхронізовано endpoint/docs по platform perimeter (`AdminOutbox`, `/metrics`, `/hangfire`, системні health routes).

## Blockers

Blockers закриті.

## Near-term

- Моніторити `platform-gate` coverage trend і тримати поріг не нижче `12%` при змінах у jobs/outbox/idempotency.
- Розширити synthetic перевірки на `outbox-dispatch`/`payments-sync-pending` для раннього виявлення деградацій.

## Optional

- ~~Додати trace-level distributed telemetry.~~ **Done** — OTLP traces → Jaeger; див. [docs/platform-engineering/](../../docs/platform-engineering/README.md).
- ~~Додати окремий operational dashboard по DLQ/idempotency TTL backlog.~~ **Done** — Grafana dashboard `platform-dlq` (profile `observability`).

## Мінімальний checklist

- [x] Critical admin/debug endpoints не публічні.
- [x] Outbox failure/retry flow покритий тестами.
- [x] CI має окремий `platform-gate` з coverage threshold.
- [x] Метрики та алерти для jobs налаштовані.
