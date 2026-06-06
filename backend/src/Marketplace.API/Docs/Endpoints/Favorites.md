# Favorites Controller (`/me/favorites`)

## `GET /me/favorites`

- **Summary (1 рядок):** Список улюблених товарів користувача.
- **Призначення:** отримати список улюблених товарів поточного користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Read-through кеш `favorites:user:{userId}:list`
  3. Повернути масив `FavoriteItemDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Favorites
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** без параметрів.
- **Повертає:** масив `FavoriteItemDto`.
- **Кеш:** key `favorites:user:{userId}:list`, TTL **3 хв**.
- **Observability labels:** `favorites_list`.
- **Помилки:** `401` (без токена), `400` (валідація/pipeline).

## `POST /me/favorites/{productId}`

- **Summary (1 рядок):** Додати товар в улюблене (ідемпотентно).
- **Призначення:** додати товар в улюблене (ідемпотентно).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити існування продукту
  2. Створити або реактивувати favorite запис
  3. Інвалідувати кеш списку
- **Side effects (синхронно):** insert/reactivate favorite; інвалідація кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail / Catalog (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** `productId` у route.
- **Повертає:** `FavoriteItemDto` (новий або вже існуючий запис).
- **Ідемпотентність:** повторний POST повертає той самий активний запис; після soft-delete запис реактивується.
- **Кеш:** інвалідує `favorites:user:{userId}:list`.
- **Observability labels:** `favorites_add`.
- **Помилки:** `404` (`Product not found`), `400`.

## `DELETE /me/favorites/{productId}`

- **Summary (1 рядок):** Прибрати товар з улюбленого (ідемпотентно).
- **Призначення:** прибрати товар з улюбленого (ідемпотентно).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти favorite запис
  2. Soft-delete (або no-op якщо вже видалено)
  3. Інвалідувати кеш списку
- **Side effects (синхронно):** soft-delete існуючого запису; інвалідація кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Favorites (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** `productId` у route.
- **Повертає:** `true`.
- **Кеш:** інвалідує `favorites:user:{userId}:list`.
- **Observability labels:** `favorites_remove`.

## Observability

- Метрики: `favorites_operations_total`, `favorites_errors_total`, `favorites_latency_ms`.
- Основні operations labels: `favorites_list`, `favorites_add`, `favorites_remove`.
