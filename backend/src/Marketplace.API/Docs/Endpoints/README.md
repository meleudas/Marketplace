# Довідник endpoint-ів

Кожен файл відповідає одному контролеру (або групі маршрутів). Текст звідси автоматично потрапляє в **Swagger** (`/swagger`) та **Scalar** (`/scalar`) через `OpenApi/EndpointDocRegistry`.

## Формат одного endpoint-а

Заголовок секції (підтримуються `##`, `###`, `####`):

```markdown
### `POST /me/cart/checkout`
```

Обов'язкові поля (bullets) у форматі `- **Назва поля:** значення` (двокрапка **всередині** bold, як у Markdown):

| Поле | Призначення |
|------|-------------|
| **Summary (1 рядок)** | Короткий заголовок операції в OpenAPI |
| **Призначення** | Що робить endpoint |
| **Хто може викликати** | JWT, глобальні ролі, компанійні ролі |
| **Бізнес-логіка** | Покроково, що відбувається в handler |
| **Side effects (синхронно)** | БД, кеш, idempotency store |
| **Async / «магія»** | Outbox, Hangfire, push/in-app/email (див. [EventCatalog](../Notifications/EventCatalog.md)) |
| **Де на фронті** | Екран, API-модуль, `Статус: implemented/planned/not_implemented` |
| **Приймає** | route / query / body |
| **Повертає** | успішна відповідь |
| **Помилки** | типові `detail`/статуси |
| **Примітки** | ідемпотентність, метрики, інваріанти |

### Приклад

```markdown
## `POST /me/cart/checkout`

- **Summary (1 рядок):** Оформлення замовлення зі сплітом по компаніях.
- **Призначення:** checkout активного кошика з оплатою та доставкою.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Buyer, Seller, Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідація кошика, адреси та методу оплати
  2. Спліт позицій по `CompanyId` → N orders
  3. Резервування інвентарю та ініціалізація LiqPay (якщо не cash)
- **Side effects (синхронно):** orders, order_items, order_addresses, idempotency store, cache invalidate
- **Async / «магія»:**
  - Notifications: `AdminNewOrder`, `CompanyNewOrder`
  - Outbox: події замовлення для downstream consumers
- **Де на фронті:**
  - Екран: Checkout (planned)
  - API-модуль: `frontend/src/features/checkout/api/checkout.api.ts` (planned)
  - Статус: `planned`
- **Приймає (body):** `paymentMethod`, `shippingMethodId`, `address`, `notes?`
- **Повертає:** `CheckoutResultDto`
- **Помилки:** `400`, `404`, `409` (idempotency)
- **Idempotency:** обов'язковий заголовок `Idempotency-Key`
```

Додаткові джерела при написанні:

- [RoleAccessMatrix](../DDD/RoleAccessMatrix.md) — ролі
- [BusinessFlows](../DDD/BusinessFlows.md) — сценарії
- [FrontendCoverage.md](FrontendCoverage.md) — мапа endpoint → UI

## Індекс контролерів

| Файл | Базовий шлях |
|------|----------------|
| [Auth.md](Auth.md) | `/auth` |
| [ExternalAuth.md](ExternalAuth.md) | `/auth` (Google) |
| [Account.md](Account.md) | `/account` |
| [Users.md](Users.md) | `/users` |
| [AdminCatalog.md](AdminCatalog.md) | `/admin` |
| [AdminProducts.md](AdminProducts.md) | `/admin/products` |
| [AdminOutbox.md](AdminOutbox.md) | `/admin/outbox` |
| [Catalog.md](Catalog.md) | `/catalog` |
| [Products.md](Products.md) | `/companies/{companyId}/products` |
| [Inventory.md](Inventory.md) | `/companies/{companyId}` (склади та інвентар) |
| [CompanyMembers.md](CompanyMembers.md) | `/companies/{companyId}/members` |
| [Cart.md](Cart.md) | `/me/cart` |
| [Coupons.md](Coupons.md) | `/admin/coupons`, `/me/cart/coupons` |
| [Reports.md](Reports.md) | `/reports`, `/me/reports`, `/admin/reports/*` |
| [Analytics.md](Analytics.md) | `/analytics/*` |
| [AdminAnalytics.md](AdminAnalytics.md) | `/admin/analytics/kpi/*` |
| [Shipping.md](Shipping.md) | `/me/addresses`, `/shipping`, `/me/shipments`, `/integrations/shipping/novaposhta` |
| [Favorites.md](Favorites.md) | `/me/favorites` |
| [MeNotifications.md](MeNotifications.md) | `/me/in-app-notifications` |
| [Reviews.md](Reviews.md) | `/products/*/reviews`, `/companies/*/reviews`, `/reviews/*`, `/admin/reviews/*` |
| [AdminPayments.md](AdminPayments.md) | `/admin/payments` |
| [Orders.md](Orders.md) | `/me/orders`, `/companies/{companyId}/orders`, `/admin/orders`, `/orders/{orderId}/*` |
| [PaymentsIntegrations.md](PaymentsIntegrations.md) | `/integrations/liqpay` |
| [TelegramIntegrations.md](TelegramIntegrations.md) | `/integrations/telegram` |
| [PushNotifications.md](PushNotifications.md) | `/web-push`, `/me/web-push` |

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
- **Авторизація:** лише development + `Admin`.

### `GET /health/liqpay/config`

- **Призначення:** liveness перевірка наявності LiqPay ключів у конфігурації.
- **Повертає:** healthy/unhealthy JSON.

### `GET /health/liqpay`

- **Призначення:** readiness перевірка доступності LiqPay API.
- **Повертає:** healthy/unhealthy JSON.

### `GET /metrics`

- **Призначення:** Prometheus scrape endpoint для OpenTelemetry metrics.
- **Містить:** HTTP, runtime, cache, payment/webhook, Hangfire counters/histograms.
- **Авторизація:** лише `Admin`.

### `GET /hangfire`

- **Призначення:** Hangfire dashboard для jobs/manual operations.
- **Авторизація:** endpoint піднімається лише в development середовищі.
