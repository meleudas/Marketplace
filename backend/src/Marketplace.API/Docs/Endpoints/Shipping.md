# Shipping Endpoints

## `GET /me/addresses`

- **Summary (1 рядок):** Збережені адреси поточного користувача.
- **Призначення:** отримати збережені адреси поточного користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Завантажити адреси користувача
  3. Повернути список `UserAddressDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / address book (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** список `UserAddressDto`.

## `POST /me/addresses`

- **Summary (1 рядок):** Створення нової адреси користувача.
- **Призначення:** створити адресу користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати адресні поля
  2. Створити `UserAddress` (з урахуванням `isDefault`)
  3. Повернути `UserAddressDto`
- **Side effects (синхронно):** новий запис адреси; можливе скидання попередньої default
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / add address (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає (body):**
  - `type` (`shipping` / `billing` / `both`)
  - `isDefault`
  - `firstName`, `lastName`, `phone`
  - `street`, `city`, `state`, `postalCode`, `country`
- **Повертає:** створений `UserAddressDto`.

## `PATCH /me/addresses/{addressId}`

- **Summary (1 рядок):** Оновлення адреси користувача.
- **Призначення:** оновити адресу користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник адреси
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти адресу за `addressId` (власник = поточний user)
  2. Застосувати оновлення
  3. Повернути оновлений `UserAddressDto`
- **Side effects (синхронно):** оновлення запису адреси
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / edit address (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** оновлений `UserAddressDto`.

## `DELETE /me/addresses/{addressId}`

- **Summary (1 рядок):** М'яке видалення адреси користувача.
- **Призначення:** видалити адресу користувача (soft-delete).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник адреси
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти адресу власника
  2. Виконати soft-delete
- **Side effects (синхронно):** soft-delete адреси
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / address book (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** `200 OK`.

## `POST /me/addresses/{addressId}/default`

- **Summary (1 рядок):** Встановлення адреси за замовчуванням.
- **Призначення:** встановити адресу за замовчуванням.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник адреси
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти адресу власника
  2. Скинути попередню default-адресу
  3. Встановити `isDefault = true` для вибраної
- **Side effects (синхронно):** оновлення default-інваріанту
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout / address book (planned)
  - API-модуль: —
  - Статус: `planned`
- **Інваріант:** тільки одна активна default-адреса на користувача.

## `GET /shipping/methods`

- **Summary (1 рядок):** Доступні методи доставки.
- **Призначення:** отримати доступні методи доставки.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити активні shipping methods
  2. Повернути список `ShippingMethodDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** список `ShippingMethodDto`.

## `POST /shipping/quote`

- **Summary (1 рядок):** Розрахунок shipping quote зі snapshot.
- **Призначення:** розрахувати shipping quote і зберегти його snapshot.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `shippingMethodId` та адресу
  2. Розрахувати вартість та ETA
  3. Зберегти snapshot quote з `expiresAtUtc`
- **Side effects (синхронно):** запис shipping quote snapshot
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Checkout (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає (body):** `shippingMethodId` + контакт/адреса.
- **Повертає:** `ShippingQuoteDto` (`quoteId`, сума, ETA, `expiresAtUtc`).

## `GET /me/shipments`

- **Summary (1 рядок):** Відправлення поточного користувача.
- **Призначення:** отримати shipment-и поточного користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Завантажити shipments користувача
  3. Повернути список `ShipmentDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: My Orders / shipments (planned)
  - API-модуль: —
  - Статус: `planned`
- **Повертає:** список `ShipmentDto`.

## `GET /me/shipments/{shipmentId}`

- **Summary:** Деталі shipment для buyer з `items[]` та `events[]` timeline.
- **Повертає:** `ShipmentDetailDto`.

## `GET /me/orders/{orderId}/shipments`

- **Summary:** Список shipments замовлення для buyer.
- **Повертає:** `ShipmentSummaryDto[]`.

## `POST /companies/{companyId}/orders/{orderId}/shipments`

- **Summary:** Seller створює partial/full shipment з line items, optional `warehouseId` та tracking.
- **Body:** `warehouseId` (обов'язковий для замовлень з multi-warehouse allocations), `lines[]`, `trackingNumber`.
- **Idempotency-Key:** обов'язковий.
- **Повертає:** `ShipmentDetailDto`.

## Multi-warehouse fulfillment

- Checkout резервує stock greedy за `Warehouse.Priority` (DESC); зберігає `order_fulfillment_allocations`.
- `FulfillmentReadinessDto.pendingByWarehouse[]` групує залишок для відправки по складах.
- Кожен shipment прив'язаний до `warehouseId`; ship зменшує stock лише на цьому складі.

## `GET /companies/{companyId}/orders/{orderId}/shipments`

- **Summary:** Список shipments замовлення для seller.

## `GET /companies/{companyId}/shipments/{shipmentId}`

- **Summary:** Деталі shipment для seller з events timeline.

## Fulfillment у order detail

`fulfillment` (`FulfillmentReadinessDto`) у `OrderDetailsDto`: прогрес partial ship без нових `OrderStatus`. Поле `pendingByWarehouse` — групи рядків по складах для seller UI.

## `POST /integrations/shipping/novaposhta/webhook`

- **Summary (1 рядок):** Webhook статусів доставки від Nova Poshta.
- **Призначення:** прийняти webhook від Nova Poshta.
- **Хто може викликати:**
  - JWT: не потрібна (інтеграційний endpoint)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Прийняти JSON payload
  2. Dedup за `(carrierCode, eventKey, payloadHash)`
  3. Оновити статус shipment/order
- **Side effects (синхронно):** оновлення shipment статусу
- **Async / «магія»:** можлива нотифікація `UserOrderStatus` покупцю
- **Де на фронті:**
  - Екран: —
  - API-модуль: —
  - Статус: `planned` (server-to-server only)
- **Idempotency:** dedup за `(carrierCode, eventKey, payloadHash)`.
- **Приймає:** довільний JSON payload + заголовок `X-NovaPoshta-Event-Id` (optional).
