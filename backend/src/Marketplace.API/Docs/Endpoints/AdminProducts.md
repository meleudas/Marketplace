# Модерація товарів (`/admin/products`)

Базовий шлях контролера: **`/admin/products`**. Доступ: ролі **`Admin`** або **`Moderator`** (JWT claim `role`).

## `GET /admin/products/pending`

- **Summary (1 рядок):** Черга товарів на модерацію.
- **Призначення:** список товарів у статусі `PendingReview`.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin або Moderator
  2. Завантажити товари зі статусом `PendingReview`
  3. Повернути масив `PendingProductModerationDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / product moderation queue (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** масив `PendingProductModerationDto` (`productId`, `companyId`, `name`, `slug`, `submittedByUserId`, `createdAt`).
- **Metrics:** `product_latency_ms{operation="admin_products_pending"}`, `product_operations_total{operation="admin_products_pending",status="success"}`, `product_errors_total{operation="admin_products_pending",reason=*}`.

## `POST /admin/products/{id}/approve`

- **Summary (1 рядок):** Схвалення товару — статус Active.
- **Призначення:** схвалити товар → статус `Active`, оновлення пошукового індексу, нотифікація автору подання (якщо відомий `submittedByUserId`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти товар у статусі `PendingReview`
  2. Встановити статус `Active`
  3. Інвалідувати кеш каталогу та поставити upsert у search index
- **Side effects (синхронно):** оновлення статусу, інвалідація `CatalogCacheKeys`
- **Async / «магія»:**
  - `IProductSearchIndexDispatcher.EnqueueUpsertProductAsync`
  - In-app/Web Push нотифікація автору (див. [EventCatalog.md](../Notifications/EventCatalog.md))
- **Де на фронті:**
  - Екран: AdminShell / approve product (planned)
  - API-модуль: —
  - Статус: `partial`
- **Параметри:** `id` — ідентифікатор товару.
- **Metrics:** `product_latency_ms{operation="admin_products_approve"}`, `product_operations_total{operation="admin_products_approve",status="success"}`, `product_errors_total{operation="admin_products_approve",reason="unauthorized|not_found|application_failure"}`.

## `POST /admin/products/{id}/reject`

- **Summary (1 рядок):** Відхилення товару — повернення в Draft.
- **Призначення:** відхилити товар → статус `Draft`, збереження причини, нотифікація автору.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти товар у статусі `PendingReview`
  2. Встановити статус `Draft` та зберегти `reason`
  3. Інвалідувати кеш каталогу
- **Side effects (синхронно):** оновлення статусу, інвалідація `CatalogCacheKeys`
- **Async / «магія»:** in-app/Web Push нотифікація автору (див. [EventCatalog.md](../Notifications/EventCatalog.md))
- **Де на фронті:**
  - Екран: AdminShell / reject product (planned)
  - API-модуль: —
  - Статус: `partial`
- **Body (опційно):** `{ "reason": "..." }`.
- **Metrics:** `product_latency_ms{operation="admin_products_reject"}`, `product_operations_total{operation="admin_products_reject",status="success"}`, `product_errors_total{operation="admin_products_reject",reason="unauthorized|not_found|application_failure"}`.

## Side effects (загальні)

- Інвалідація кешу каталогу (`CatalogCacheKeys`).
- Після схвалення: `IProductSearchIndexDispatcher.EnqueueUpsertProductAsync`.
- Нотифікації: див. [EventCatalog.md](../Notifications/EventCatalog.md) (розділ «Товари та модерація»).
