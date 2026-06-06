# Analytics endpoints

## `POST /analytics/events`

- **Summary (1 рядок):** Ingestion подій каталогу (view/search/click/add-to-cart).
- **Призначення:** ingestion подій каталогу (`view/search/click/add-to-cart`).
- **Хто може викликати:**
  - JWT: не обов'язкова (анонімно або автентифіковано)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати payload події
  2. Записати подію в analytics store
  3. Повернути успіх або rate-limit помилку
- **Side effects (синхронно):** запис analytics event
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Storefront tracking hooks (planned)
  - API-модуль: —
  - Статус: `planned`
- **Помилки:** `400`, `413`, `429`, `503`.

## `POST /analytics/search-history`

- **Summary (1 рядок):** Tracking пошукової історії користувача.
- **Призначення:** tracking пошукової історії.
- **Хто може викликати:**
  - JWT: не обов'язкова (анонімно або автентифіковано)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати search query payload
  2. Записати запис search-history
  3. Повернути успіх
- **Side effects (синхронно):** запис search-history
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: ProductsScreen search tracking (planned)
  - API-модуль: —
  - Статус: `planned`
- **Помилки:** `400`, `413`, `429`, `503`.
