# Domain Report: Cart & Checkout

- Статус реалізації: `Implemented`
- Готовність: **90/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `cart-checkout-gate`
- Container: CheckoutPostgresTests

## Межі домену

- `backend/src/Marketplace.Domain/Cart`
- `backend/src/Marketplace.Application/Carts`
- `backend/src/Marketplace.API/Controllers/CartController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Cart*`

## Що вже готово

- Повний CRUD кошика і checkout у замовлення.
- Ідемпотентність для checkout endpoint.
- Інтеграція зі stock-watch і частиною notification flow.
- Транзакційний checkout (`IAppTransactionPort`) з rollback при fail-path.
- Додані метрики/логування для cart mutations і checkout.
- Додано `CartCheckout` quality gate у CI.
- Додано інтеграційні SQLite e2e тести на success/rollback/idempotency-store replay.

## Blockers

- Немає (закрито).

## Near-term

- Моніторити стабільність нового `CartCheckout` CI gate протягом 1-2 спринтів.
- Зафіксувати SLO checkout latency у прод-дашборді.

## Optional

- Додати abandoned cart автоматизацію.
- Додати промокоди/знижки у checkout pipeline.

## Мінімальний checklist

- [x] Checkout повторюваний і без дублювання замовлень.
- [x] Deficit stock не створює часткових записів.
- [x] Cart API має покриття позитивних/негативних сценаріїв.
- [x] Всі побічні ефекти checkout логуються і відслідковуються.
