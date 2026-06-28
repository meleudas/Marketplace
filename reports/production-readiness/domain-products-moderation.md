# Domain Report: Products & Moderation

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Catalog`
- `backend/src/Marketplace.Application/Products`
- `backend/src/Marketplace.API/Controllers/ProductsController.cs`
- `backend/src/Marketplace.API/Controllers/AdminProductsController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Product*`

## Що вже готово

- Закритий moderation lifecycle `create/update(draft) -> pending -> approve/reject` з edge-handling для invalid transitions/not-found/forbidden сценаріїв.
- Зафіксовані side effects moderation: cache invalidation, search indexing на approve, deterministic notification correlation IDs для dedup/replay-safe поведінки.
- Розширено test contour до production-ready рівня: `domain + application handlers + API + SQLite integration + contract + security + performance` з профільним `Suite=ProductsModeration`.
- Додано окремий `products-moderation-gate` у CI (`Threshold=12`) з coverlet include filter для products surface (`API/Application/Domain/Infrastructure`).
- Додано product observability trio (`product_operations_total`, `product_errors_total`, `product_latency_ms`) та інструментовано `ProductsController` і `AdminProductsController`.
- Оновлені endpoint/docs (`Products`, `AdminProducts`, `ProductsContext`, `ObservabilityRunbook`) під фактичний moderation-flow.

## Blockers

Blockers закриті.

## Near-term

- Моніторити `products-moderation-gate` coverage trend і не опускати нижче `12%`.
- Тримати алерти на `product_errors_total`/`product_latency_ms` в operational baseline після релізів.

## Optional

- Додати bulk moderation та reason templates.
- Розширити audit подій модерації.

## Мінімальний checklist

- [x] approve/reject мають API security coverage.
- [x] Upload path має валідацію і чіткі помилки.
- [x] CI має окремий `products-moderation-gate` з coverage threshold.
- [x] Pending товари не потрапляють у публічний каталог.
