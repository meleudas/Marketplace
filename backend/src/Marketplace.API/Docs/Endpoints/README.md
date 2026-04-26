# Довідник endpoint-ів

Кожен файл відповідає одному контролеру (або групі маршрутів). Формат одного endpoint-а:

- **Маршрут** — метод і path
- **Призначення** — що робить
- **Приймає** — route / query / body
- **Повертає** — успішна відповідь
- **Авторизація** — хто може викликати
- **Side effects** — зміни в БД, кеш, рухи складу тощо
- **Помилки** — типові `detail`/статуси
- **Примітки** — ідемпотентність, інваріанти

## Індекс контролерів

| Файл | Базовий шлях |
|------|----------------|
| [Auth.md](Auth.md) | `/auth` |
| [ExternalAuth.md](ExternalAuth.md) | `/auth` (Google) |
| [Account.md](Account.md) | `/account` |
| [Users.md](Users.md) | `/users` |
| [AdminCatalog.md](AdminCatalog.md) | `/admin` |
| [Catalog.md](Catalog.md) | `/catalog` |
| [Products.md](Products.md) | `/companies/{companyId}/products` |
| [Inventory.md](Inventory.md) | `/companies/{companyId}` (склади та інвентар) |
| [CompanyMembers.md](CompanyMembers.md) | `/companies/{companyId}/members` |
| [Cart.md](Cart.md) | `/me/cart` |
| [Favorites.md](Favorites.md) | `/me/favorites` |
| [Reviews.md](Reviews.md) | `/products/*/reviews`, `/companies/*/reviews`, `/reviews/*`, `/admin/reviews/*` |
| [AdminPayments.md](AdminPayments.md) | `/admin/payments` |
| [Orders.md](Orders.md) | `/me/orders`, `/companies/{companyId}/orders`, `/admin/orders`, `/orders/{orderId}/*` |
| [PaymentsIntegrations.md](PaymentsIntegrations.md) | `/integrations/liqpay` |
| [TelegramIntegrations.md](TelegramIntegrations.md) | `/integrations/telegram` 

## Системні маршрути (не з контролерів, `Program.cs`)

### `GET /health`

- **Призначення:** liveness.
- **Повертає:** `{ "status": "ok" }`.
- **Авторизація:** немає.

### `GET /health/sendgrid`

- **Призначення:** перевірка доступності SendGrid.
- **Повертає:** JSON від `IEmailHealthProbe`.
- **Помилки:** **503** `ProblemDetails`, якщо провайдер «нездоровий».

### `GET /health/sendgrid/key-trace`

- **Призначення:** діагностика джерел API-ключа (масковано).
- **Авторизація:** немає в коді — **не відкривати в публічному інтернеті без захисту**.

### `GET /health/liqpay/config`

- **Призначення:** liveness перевірка наявності LiqPay ключів у конфігурації.
- **Повертає:** healthy/unhealthy JSON.

### `GET /health/liqpay`

- **Призначення:** readiness перевірка доступності LiqPay API.
- **Повертає:** healthy/unhealthy JSON.

### `GET /metrics`

- **Призначення:** Prometheus scrape endpoint для OpenTelemetry metrics.
- **Містить:** HTTP, runtime, cache, payment/webhook, Hangfire counters/histograms.
