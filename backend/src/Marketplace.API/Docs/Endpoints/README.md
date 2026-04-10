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
| [TelegramIntegrations.md](TelegramIntegrations.md) | `/integrations/telegram` |

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
