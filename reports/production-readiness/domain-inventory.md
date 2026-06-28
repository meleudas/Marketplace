# Domain Report: Inventory

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Inventory`
- `backend/src/Marketplace.Application/Inventory`
- `backend/src/Marketplace.API/Controllers/InventoryController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/WarehouseStockRepository.cs`
- `backend/src/Marketplace.Infrastructure/Jobs/InventoryJobs.cs`

## Що вже готово

- Команди reserve/release/ship/receive/transfer/adjust.
- Обробка конкурентності та повторних операцій через operation-id.
- Integration з checkout/orders і частково з notifications.
- Додано розширений test contour: application handler тести для reserve/release/ship/receive/transfer, API тести `InventoryController`, SQLite integration сценарії reserve->release, transfer і expiration job.
- Уніфіковано тестову маркування через `Suite=Inventory` для профільних inventory-тестів (domain/app/api/integration/restock).
- Додано окремий CI quality gate `inventory-gate` з coverage threshold.
- Інструментовано `InventoryJobs.ExpireReservationsAsync` метриками (`hangfire_jobs_total`, `hangfire_job_errors_total`, `hangfire_job_latency_ms`) з тегом `job=inventory-expire-reservations`.
- Оновлено observability runbook для inventory-specific SLI/alerts.

## Blockers

- Blockers закриті.

## Near-term

- Підтримувати `Suite=Inventory` при еволюції команд і не знижувати coverage gate.
- Розширити навантажувальні сценарії для high-throughput reserve/release у performance-контурі.

## Optional

- Впровадити сценарії chaos/retry для складських операцій.
- Додати performance-baseline на high-throughput reserve/release.

## Мінімальний checklist

- [x] Усі inventory-команди мають unit + negative tests.
- [x] Є concurrency/idempotency покриття для reserve/transfer flow.
- [x] Налаштовані метрики/алерти на inventory jobs.
- [x] Доступ до inventory API обмежений ролями компанії.
