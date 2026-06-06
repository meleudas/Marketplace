# Orders Controller

## `GET /me/orders`

- **Summary (1 рядок):** Список моїх замовлень (buyer scope).
- **Призначення:** пагінований список замовлень поточного покупця з фільтрами.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Buyer, Seller, Admin (admin бачить через scope My лише свої як buyer)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити buyer `userId`
  2. Read-through list cache
  3. Застосувати фільтри statuses/date/search/sort/page
- **Side effects (синхронно):** лише read (+ cache populate)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: My Orders (planned)
  - API-модуль: `frontend/src/features/orders/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `statuses[]`, `createdFromUtc`, `createdToUtc`, `search`, `sort`, `page`, `pageSize`
- **Повертає:** paginated orders list
- **Помилки:** `401`, `400`
- **Metrics:** `order_latency_ms{operation="orders_list_my"}`

## `GET /me/orders/{orderId}`

- **Summary (1 рядок):** Деталі мого замовлення з timeline статусів.
- **Призначення:** повертає order detail для buyer, якщо він власник.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: buyer-власник або Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити доступ buyer/admin
  2. Read-through detail cache `orders:detail:{orderId}`
  3. Зібрати DTO з `statusHistory[]`
- **Side effects (синхронно):** read-only
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Order details (planned)
  - API-модуль: `frontend/src/features/orders/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `orderId` (route)
- **Повертає:** order detail + `statusHistory[]`
- **Помилки:** `404`, `403`, `401`

## `GET /companies/{companyId}/orders`

- **Summary (1 рядок):** Список замовлень компанії (seller scope).
- **Призначення:** замовлення, де `companyId` = продавець; доступ для членів компанії або Admin.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin (bypass)
  - Компанійні ролі: Owner, Manager, Seller, Support, Logistics (read)
- **Бізнес-логіка:**
  1. Перевірити company membership / admin bypass
  2. Фільтрувати orders by companyId
  3. Пагінація та sort/filter
- **Side effects (синхронно):** read-only + list cache
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace → Orders (planned)
  - API-модуль: `frontend/src/features/workspace/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `companyId`, query filters
- **Повертає:** paginated company orders
- **Помилки:** `403`, `401`, `404`

## `GET /companies/{companyId}/orders/{orderId}`

- **Summary (1 рядок):** Деталі замовлення в межах компанії-продавця.
- **Призначення:** seller view order detail + status timeline.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin
  - Компанійні ролі: член компанії з read-доступом
- **Бізнес-логіка:**
  1. Company access check
  2. Load order belonging to company
  3. Map timeline/history
- **Side effects (синхронно):** read-only
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace → Order details (planned)
  - API-модуль: `frontend/src/features/workspace/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `companyId`, `orderId`
- **Повертає:** order detail + `statusHistory[]`
- **Помилки:** `403`, `404`, `401`

## `GET /admin/orders`

- **Summary (1 рядок):** Адміністраторський список усіх замовлень.
- **Призначення:** platform-wide orders list для підтримки та аналітики.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірка ролі Admin
  2. List query без buyer/company scope обмежень
- **Side effects (синхронно):** read-only + admin list cache
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Admin → Orders (planned)
  - API-модуль: `frontend/src/features/admin/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** query filters
- **Повертає:** paginated admin orders
- **Помилки:** `403`, `401`

## `GET /admin/orders/{orderId}`

- **Summary (1 рядок):** Адміністраторські деталі замовлення.
- **Призначення:** повний перегляд order + audit timeline для Admin.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Admin role check
  2. Load order by id без company/buyer обмежень
- **Side effects (синхронно):** read-only
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Admin → Order details (planned)
  - API-модуль: `frontend/src/features/admin/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `orderId`
- **Повертає:** order detail + `statusHistory[]`
- **Помилки:** `404`, `403`, `401`

## `POST /orders/{orderId}/status`

- **Summary (1 рядок):** Змінити статус замовлення (Processing/Shipped/Delivered).
- **Призначення:** seller або admin оновлює fulfillment status; опційно tracking number.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin
  - Компанійні ролі: Owner, Manager, Seller, Logistics (write policy)
- **Бізнес-логіка:**
  1. Idempotency guard
  2. Validate status transition
  3. Persist new status + audit history
  4. Invalidate order caches (my/company/admin scopes)
- **Side effects (синхронно):** update order status/history, cache version bump
- **Async / «магія»:**
  - Outbox: `OrderStatusChanged`
  - Notifications: `UserOrderStatus` покупцю (Push + InApp)
- **Де на фронті:**
  - Екран: Workspace → Order actions (planned)
  - API-модуль: `frontend/src/features/workspace/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає (body):** `status`, `trackingNumber?`
- **Повертає:** updated order/status DTO
- **Помилки:** `400`, `403`, `404`, `409` (idempotency)
- **Idempotency:** обов'язковий `Idempotency-Key`

## `POST /orders/{orderId}/cancel`

- **Summary (1 рядок):** Скасувати замовлення за політикою доступу.
- **Призначення:** buyer/seller/admin може скасувати order, якщо дозволено політикою статусів.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: Admin, buyer-власник
  - Компанійні ролі: seller компанії замовлення (за політикою)
- **Бізнес-логіка:**
  1. Idempotency guard
  2. Access policy (buyer/seller/admin)
  3. Set status `Cancelled` + audit trail
  4. Release inventory reservations (якщо застосовно)
  5. Cache invalidation
- **Side effects (синхронно):** status update, inventory release, cache bump
- **Async / «магія»:**
  - Outbox: `OrderCancelled`
  - Notifications: `UserOrderStatus` з `Cancelled`
- **Де на фронті:**
  - Екран: My Orders / Workspace Orders (planned)
  - API-модуль: `frontend/src/features/orders/api/orders.api.ts` (planned)
  - Статус: `planned`
- **Приймає:** `orderId`, optional cancel reason (body policy-dependent)
- **Повертає:** updated order
- **Помилки:** `400`, `403`, `404`, `409`
- **Idempotency:** обов'язковий `Idempotency-Key`
