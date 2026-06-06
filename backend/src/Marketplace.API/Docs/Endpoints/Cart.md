# Cart Controller (`/me/cart`)

## `GET /me/cart`

- **Summary (1 рядок):** Отримати активний кошик поточного користувача.
- **Призначення:** повертає активний кошик з позиціями, сумою та кількістю; при відсутності створює порожній.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Read-through кеш `cart:user:{userId}:active`
  3. Якщо кошика немає — створити порожній активний cart
- **Side effects (синхронно):** можливе створення порожнього cart у БД
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Cart (planned)
  - API-модуль: `frontend/src/features/cart/api/cart.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** без параметрів
- **Повертає:** `CartDto` з items, total item count, total amount
- **Помилки:** `401`, `400`

## `POST /me/cart/items`

- **Summary (1 рядок):** Додати товар у кошик.
- **Призначення:** додає позицію або інкрементує quantity для існуючого `productId`.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Buyer, Seller, Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти/створити активний cart
  2. Перевірити існування продукту
  3. Додати item або збільшити quantity
  4. Синхронізувати `cart_stock_watches`, якщо qty > available
- **Side effects (синхронно):** insert/update cart_items, invalidate cart cache
- **Async / «магія»:**
  - Можлива реєстрація `cart_stock_watches` для restock notify (див. EventCatalog)
- **Де на фронті:**
  - Екран: Product detail / Catalog (planned)
  - API-модуль: `frontend/src/features/cart/api/cart.api.ts` (planned)
  - Статус: `planned`
- **Приймає (body):** `productId`, `quantity`
- **Повертає:** оновлений `CartDto`
- **Помилки:** `404` (`Product not found`), `400`, `401`

## `PATCH /me/cart/items/{itemId}`

- **Summary (1 рядок):** Оновити кількість позиції кошика.
- **Призначення:** змінює quantity конкретного cart item.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: автентифікований власник кошика
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти активний cart користувача
  2. Оновити quantity item за `itemId`
  3. Перерахувати totals і stock watches
- **Side effects (синхронно):** update cart_items, invalidate cache
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Cart (planned)
  - API-модуль: `frontend/src/features/cart/api/cart.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `itemId` (route), `quantity` (body)
- **Повертає:** оновлений `CartDto`
- **Помилки:** `404`, `400`, `401`

## `DELETE /me/cart/items/{itemId}`

- **Summary (1 рядок):** Видалити позицію з кошика.
- **Призначення:** soft-delete однієї позиції активного кошика.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник кошика
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти cart item у активному кошику
  2. Soft-delete позиції
  3. Зняти stock watch для цього SKU
- **Side effects (синхронно):** soft-delete cart item, invalidate cache
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Cart (planned)
  - API-модуль: `frontend/src/features/cart/api/cart.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `itemId` (route)
- **Повертає:** оновлений `CartDto`
- **Помилки:** `404`, `401`

## `DELETE /me/cart`

- **Summary (1 рядок):** Очистити активний кошик.
- **Призначення:** soft-delete всіх позицій активного кошика.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник кошика
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти активний cart
  2. Soft-delete всі items
  3. Очистити stock watches
- **Side effects (синхронно):** mass soft-delete items, invalidate cache
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Cart (planned)
  - API-модуль: `frontend/src/features/cart/api/cart.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** без параметрів
- **Повертає:** порожній `CartDto`
- **Помилки:** `404`, `401`

## `POST /me/cart/checkout`

- **Summary (1 рядок):** Checkout зі сплітом кошика на ордери по компаніях.
- **Призначення:** оформити замовлення з адресою, доставкою, оплатою та купоном (якщо застосовано).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Buyer, Seller, Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Idempotency guard за `Idempotency-Key`
  2. Валідація cart, address, shipping method, payment method, coupon
  3. Спліт items по `CompanyId` → N orders
  4. Резервування інвентарю та snapshot адреси
  5. Ініціалізація LiqPay для безготівкових методів
  6. Soft-delete checkout-нутих cart items
- **Side effects (синхронно):** orders, order_items, order_addresses, payments init, inventory reservations, idempotency store, cart cache invalidate
- **Async / «магія»:**
  - Notifications: `AdminNewOrder`, `CompanyNewOrder` (див. EventCatalog)
  - Outbox: події замовлення для retry-safe processing
  - Hangfire: подальша синхронізація платежів/outbox dispatch
- **Де на фронті:**
  - Екран: Checkout (planned)
  - API-модуль: `frontend/src/features/checkout/api/checkout.api.ts` (planned)
  - Статус: `planned`
- **Приймає (body):**
  - `paymentMethod` (`card` / `payPal` / `bankTransfer` / `cash`)
  - `shippingMethodId`
  - `address` (`firstName`, `lastName`, `phone`, `street`, `city`, `state`, `postalCode`, `country`)
  - `notes` (optional)
- **Повертає:** `CheckoutResultDto` з `createdOrders[]` (може містити LiqPay `payment`)
- **Помилки:** `400`, `404`, `409` (idempotency)
- **Idempotency:** обов'язковий заголовок `Idempotency-Key`
- **Спостережуваність:** `checkout_operations_total`, `checkout_errors_total`, `checkout_latency_ms`
