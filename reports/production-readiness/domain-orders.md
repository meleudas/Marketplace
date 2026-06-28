# Domain Report: Orders

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Orders`
- `backend/src/Marketplace.Application/Orders`
- `backend/src/Marketplace.API/Controllers/OrdersController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Order*`

## Що вже готово

- Повний orders test contour: розширено `domain + application + API + integration + contract + security` сценарії для lifecycle, authz та idempotency.
- Уніфіковано маркування профільних тестів через `Suite=Orders` для стабільного виконання доменного gate.
- Додано окремий `orders-gate` у CI з coverage threshold `12%` і domain-focused include фільтрами.
- Додано order-specific observability: `order_operations_total`, `order_errors_total`, `order_latency_ms` + оновлені alerts/runbook.
- Оновлено endpoint docs для Orders з expected SLI/metrics по кожній read/write операції.

## Blockers

Blockers закриті.

## Near-term

- Моніторити `orders-gate` тренд покриття і тримати поріг не нижче 12% при зміні lifecycle logic.
- Зафіксувати dashboard для `order_errors_total` з розрізом `reason` (forbidden/idempotency/application_failure).

## Optional

- Підняти orders coverage threshold вище 12% після наступного циклу hardening.
- Додати окремі synthetic checks для `orders_update_status` і `orders_cancel`.

## Мінімальний checklist

- [x] Transition rules повністю протестовані.
- [x] AuthZ матриця buyer/seller/admin покрита.
- [x] Side effects (cache, notifications) перевірені тестами.
- [x] Order endpoints мають контрактні тести.
