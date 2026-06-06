# ReviewsController — `/products/*/reviews`, `/companies/*/reviews`, `/reviews/*`, `/admin/reviews/*`

### `GET /products/{productId}/reviews?page=&size=`

- **Summary (1 рядок):** Публічний список відгуків товару.
- **Призначення:** публічний список відгуків товару з пагінацією.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити відгуки товару (read-through кеш)
  2. Застосувати пагінацію
  3. Повернути `ReviewListDto`
- **Side effects (синхронно):** читає з кешу `catalog:products:reviews:{productId}:{page}:{size}`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail reviews (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewListDto`.
- **Metrics:** `review_latency_ms{operation="reviews_list_product"}`, `review_operations_total{operation="reviews_list_product",status="success"}`, `review_errors_total{operation="reviews_list_product",reason=*}`.

### `POST /products/{productId}/reviews`

- **Summary (1 рядок):** Створення відгуку на товар (verified buyer).
- **Призначення:** створити відгук на товар.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer** (verified purchase)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити verified buyer та унікальність `(productId, userId)`
  2. Створити відгук
  3. Перерахувати `rating/reviewCount` товару; інвалідувати кеш
- **Side effects (синхронно):** перерахунок `rating/reviewCount` товару + інвалідація catalog/review кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail / write review (planned)
  - API-модуль: —
  - Статус: `planned`
- **Інваріанти:** лише verified buyer; 1 відгук на `(productId, userId)`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_create_product"}`, `review_operations_total{operation="reviews_create_product",status="success"}`, `review_errors_total{operation="reviews_create_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PATCH /products/{productId}/reviews/{reviewId}`

- **Summary (1 рядок):** Оновлення власного відгуку на товар.
- **Призначення:** оновити власний відгук на товар.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автор відгуку
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити власність відгуку
  2. Застосувати оновлення
  3. Перерахувати рейтинг; інвалідувати кеш
- **Side effects (синхронно):** оновлення відгуку; інвалідація кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail / edit review (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_update_product"}`, `review_operations_total{operation="reviews_update_product",status="success"}`, `review_errors_total{operation="reviews_update_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `DELETE /products/{productId}/reviews/{reviewId}`

- **Summary (1 рядок):** М'яке видалення власного відгуку на товар.
- **Призначення:** soft-delete власного відгуку.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автор відгуку
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити власність відгуку
  2. Soft-delete відгук
  3. Перерахувати рейтинг; інвалідувати кеш
- **Side effects (синхронно):** soft-delete; перерахунок рейтингу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `200 OK`.
- **Metrics:** `review_latency_ms{operation="reviews_delete_product"}`, `review_operations_total{operation="reviews_delete_product",status="success"}`, `review_errors_total{operation="reviews_delete_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `GET /companies/{companyId}/reviews?page=&size=`

- **Summary (1 рядок):** Публічний список відгуків компанії.
- **Призначення:** публічний список відгуків компанії.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити відгуки компанії (read-through кеш)
  2. Застосувати пагінацію
  3. Повернути `ReviewListDto`
- **Side effects (синхронно):** читає з кешу `catalog:companies:reviews:{companyId}:{page}:{size}`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Company detail reviews (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewListDto`.
- **Metrics:** `review_latency_ms{operation="reviews_list_company"}`, `review_operations_total{operation="reviews_list_company",status="success"}`, `review_errors_total{operation="reviews_list_company",reason=*}`.

### `POST /companies/{companyId}/reviews`

- **Summary (1 рядок):** Створення відгуку на компанію (verified buyer).
- **Призначення:** створити відгук на компанію.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer** (verified purchase)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити verified buyer та унікальність `(companyId, userId)`
  2. Створити відгук
  3. Перерахувати рейтинг компанії
- **Side effects (синхронно):** оновлення рейтингу компанії; інвалідація кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Company detail / write review (planned)
  - API-модуль: —
  - Статус: `planned`
- **Інваріанти:** лише verified buyer; 1 відгук на `(companyId, userId)`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_create_company"}`, `review_operations_total{operation="reviews_create_company",status="success"}`, `review_errors_total{operation="reviews_create_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PATCH /companies/{companyId}/reviews/{reviewId}`

- **Summary (1 рядок):** Оновлення власного відгуку на компанію.
- **Призначення:** оновити власний відгук на компанію.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автор відгуку
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити власність відгуку
  2. Застосувати оновлення
  3. Перерахувати рейтинг компанії
- **Side effects (синхронно):** оновлення відгуку; інвалідація кешу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Company detail (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_update_company"}`, `review_operations_total{operation="reviews_update_company",status="success"}`, `review_errors_total{operation="reviews_update_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `DELETE /companies/{companyId}/reviews/{reviewId}`

- **Summary (1 рядок):** М'яке видалення власного відгуку на компанію.
- **Призначення:** soft-delete власного відгуку на компанію.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автор відгуку
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити власність відгуку
  2. Soft-delete відгук
  3. Перерахувати рейтинг компанії
- **Side effects (синхронно):** soft-delete; перерахунок рейтингу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Company detail (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `200 OK`.
- **Metrics:** `review_latency_ms{operation="reviews_delete_company"}`, `review_operations_total{operation="reviews_delete_company",status="success"}`, `review_errors_total{operation="reviews_delete_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PUT /reviews/products/{reviewId}/reply`

- **Summary (1 рядок):** Офіційна відповідь продавця на відгук товару.
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук товару.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller** (компанії товару)
- **Бізнес-логіка:**
  1. Перевірити доступ до компанії товару
  2. Створити або оновити `ReviewReply`
  3. Повернути `ReviewReplyDto`
- **Side effects (синхронно):** upsert reply
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace reviews / reply (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewReplyDto`.
- **Metrics:** `review_latency_ms{operation="reviews_reply_product"}`, `review_operations_total{operation="reviews_reply_product",status="success"}`, `review_errors_total{operation="reviews_reply_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PUT /reviews/companies/{reviewId}/reply`

- **Summary (1 рядок):** Офіційна відповідь продавця на відгук компанії.
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller**
- **Бізнес-логіка:**
  1. Перевірити доступ до компанії
  2. Створити або оновити `ReviewReply`
  3. Повернути `ReviewReplyDto`
- **Side effects (синхронно):** upsert reply
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace reviews / reply (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `ReviewReplyDto`.
- **Metrics:** `review_latency_ms{operation="reviews_reply_company"}`, `review_operations_total{operation="reviews_reply_company",status="success"}`, `review_errors_total{operation="reviews_reply_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `POST /admin/reviews/products/{reviewId}/moderate`

- **Summary (1 рядок):** Модерація відгуку на товар.
- **Призначення:** модерація відгуку на товар.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти відгук товару
  2. Застосувати moderation action
  3. Оновити видимість/статус відгуку
- **Side effects (синхронно):** оновлення статусу відгуку; інвалідація кешу
- **Async / «магія»:** можлива нотифікація автору відгуку
- **Де на фронті:**
  - Екран: AdminShell / review moderation (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_moderate_product"}`, `review_operations_total{operation="reviews_moderate_product",status="success"}`, `review_errors_total{operation="reviews_moderate_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `POST /admin/reviews/companies/{reviewId}/moderate`

- **Summary (1 рядок):** Модерація відгуку на компанію.
- **Призначення:** модерація відгуку на компанію.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти відгук компанії
  2. Застосувати moderation action
  3. Оновити видимість/статус відгуку
- **Side effects (синхронно):** оновлення статусу відгуку; інвалідація кешу
- **Async / «магія»:** можлива нотифікація автору відгуку
- **Де на фронті:**
  - Екран: AdminShell / review moderation (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_moderate_company"}`, `review_operations_total{operation="reviews_moderate_company",status="success"}`, `review_errors_total{operation="reviews_moderate_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

## Типові помилки

- `401 Unauthorized` — відсутній або невалідний `sub` claim в authorize-запитах.
- `403 Forbidden` — недостатні права на update/delete/reply/moderate.
- `404 Not found` — review/product/company не знайдені або route scope не збігається з review target.
- `400`/`application_failure` — порушені доменні інваріанти (rating/comment/verified purchase).
