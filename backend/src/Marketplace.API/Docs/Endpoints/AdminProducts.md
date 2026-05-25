# Модерація товарів (`/admin/products`)

Базовий шлях контролера: **`/admin/products`**. Доступ: ролі **`Admin`** або **`Moderator`** (JWT claim `role`).

## `GET /admin/products/pending`

- **Призначення:** список товарів у статусі `PendingReview`.
- **Повертає:** масив `PendingProductModerationDto` (`productId`, `companyId`, `name`, `slug`, `submittedByUserId`, `createdAt`).
- **Авторизація:** `Admin`, `Moderator`.

## `POST /admin/products/{id}/approve`

- **Призначення:** схвалити товар → статус `Active`, оновлення пошукового індексу, нотифікація автору подання (якщо відомий `submittedByUserId`).
- **Параметри:** `id` — ідентифікатор товару.
- **Авторизація:** `Admin`, `Moderator`.

## `POST /admin/products/{id}/reject`

- **Призначення:** відхилити товар → статус `Draft`, збереження причини, нотифікація автору.
- **Body (опційно):** `{ "reason": "..." }`.
- **Авторизація:** `Admin`, `Moderator`.

## Side effects

- Інвалідація кешу каталогу (`CatalogCacheKeys`).
- Після схвалення: `IProductSearchIndexDispatcher.EnqueueUpsertProductAsync`.
- Нотифікації: див. [EventCatalog.md](../Notifications/EventCatalog.md) (розділ «Товари та модерація»).
