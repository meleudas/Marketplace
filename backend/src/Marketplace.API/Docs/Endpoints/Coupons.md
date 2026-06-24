# Coupons endpoints

## Admin coupons (`/admin/coupons`)

### `POST /admin/coupons`

- **Summary (1 рядок):** Створення нового купона.
- **Призначення:** створює купон.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити feature flag та права
  2. Валідувати параметри купона
  3. Створити запис і повернути `CouponDto`
- **Side effects (синхронно):** новий запис купона в БД
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / coupons (planned)
  - API-модуль: —
  - Статус: `partial`
- **Body:** `code`, `discountAmount`, `discountType`, `usageLimit`, `userUsageLimit`, `startsAtUtc`, `expiresAtUtc`, `applicableCompaniesJson`, `applicableCategoriesJson`, `applicableProductsJson`.
- **Повертає:** `CouponDto`.
- **Помилки:** `409` (дублікат коду), `422` (валідація), `503` (feature disabled).

### `PATCH /admin/coupons/{id}`

- **Summary (1 рядок):** Оновлення параметрів купона.
- **Призначення:** оновлює параметри купона.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти купон за `id`
  2. Застосувати оновлення
  3. Повернути `CouponDto`
- **Side effects (синхронно):** оновлення запису купона
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / edit coupon (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** `CouponDto`.
- **Помилки:** `404`, `422`, `503`.

### `POST /admin/coupons/{id}/deactivate`

- **Summary (1 рядок):** Аварійне вимкнення купона.
- **Призначення:** аварійне вимкнення купона.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти активний купон
  2. Деактивувати
- **Side effects (синхронно):** купон стає недоступним для застосування
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / deactivate coupon (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** `200 OK`.
- **Помилки:** `404`, `503`.

### `GET /admin/coupons/{id}/usage`

- **Summary (1 рядок):** Звіт використання купона.
- **Призначення:** usage-звіт по купону.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**, **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти купон за `id`
  2. Агрегувати `coupon_usages`
  3. Повернути `CouponUsageReportDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / coupon usage (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** `CouponUsageReportDto`.
- **Помилки:** `404`, `503`.

## Buyer cart coupons (`/me/cart/coupons`)

### `POST /me/cart/coupons/validate`

- **Summary (1 рядок):** Перевірка застосовності купона до кошика.
- **Призначення:** перевіряє застосовність купона для поточного кошика.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Buyer, Seller, Admin (автентифікований власник кошика)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити активний кошик користувача
  2. Валідувати купон за `code` (термін, ліміти, applicable companies/categories/products)
  3. Повернути `CouponValidationResultDto` без зміни кошика
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / Cart (planned)
  - API-модуль: —
  - Статус: `planned`
- **Body:** `code`.
- **Повертає:** `CouponValidationResultDto`.

### `POST /me/cart/coupons/apply`

- **Summary (1 рядок):** Застосування купона до активного кошика.
- **Призначення:** застосовує купон до активного кошика.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автентифікований власник кошика
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати купон для кошика
  2. Створити/оновити `cart_coupon_links`
  3. Повернути `CartCouponDto`
- **Side effects (синхронно):** запис зв'язку купон-кошик
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / Cart (planned)
  - API-модуль: —
  - Статус: `planned`
- **Body:** `code`.
- **Повертає:** `CartCouponDto`.

### `DELETE /me/cart/coupons/{code}`

- **Summary (1 рядок):** Зняття купона з активного кошика.
- **Призначення:** знімає купон з активного кошика.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автентифікований власник кошика
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти активний кошик
  2. Видалити `cart_coupon_links` за `code`
- **Side effects (синхронно):** видалення зв'язку купон-кошик
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / Cart (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `200 OK`.

## Checkout integration

- `POST /me/cart/checkout` автоматично враховує discount із `cart_coupon_links`.
- Перед створенням замовлень виконується **revalidation** зв'язку купон-кошик; при невалідності — `422` і auto-remove link.
- **Multi-company кошик:** знижка розподіляється пропорційно eligible subtotal по компаніях; `coupon_usages` **consume один раз** на checkout (anchor `primaryOrderId`), не на кожне замовлення окремо.
- Scope rules: якщо задано `applicableCategoriesJson` / `applicableProductsJson` — discount лише на відповідні позиції; eligible subtotal для `minOrderAmount` і відсоткової знижки рахується по eligible lines.
- Consume ідемпотентно через `coupon_usages` (`couponId + orderId` unique).
