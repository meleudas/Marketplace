# Бізнес-потоки (словами)

Короткий опис того, **що відбувається на бекенді** у типових сценаріях. Деталі HTTP — у [Endpoints/README.md](../Endpoints/README.md).

## 1. Реєстрація та перший вхід

1. Клієнт викликає **`POST /auth/register`** — створюється Identity-користувач і доменний профіль; відправляється лист підтвердження.
2. Якщо увімкнено підтвердження email, access/refresh можуть **не** видатись — користувач підтверджує пошту.
3. **`POST /account/confirm-email`** виставляє `EmailConfirmed` і позначає профіль верифікованим.
4. **`POST /auth/login`** видає JWT + refresh-cookie; оновлюється `lastLogin` у профілі.

**Залучені контексти:** [IdentityAndAccess](IdentityAndAccess.md).

---

## 2. Створення компанії та потрапляння у публічний каталог

1. Адмін створює компанію через **`POST /admin/companies`** (або інший флоу онбордингу, якщо з’явиться).
2. Доки **`isApproved = false`**, компанія **не** потрапляє в **`GET /catalog/companies`**.
3. **`POST /admin/companies/{id}/approve`** виставляє апрув і метадані (хто/коли).
4. Публічний фронт читає **`GET /catalog/companies`** — лише схвалені.

**Контексти:** [CompaniesContext](CompaniesContext.md), [CatalogContext](CatalogContext.md).

---

## 3. Запрошення команди та ролі

1. Користувач автентифікований (JWT).
2. **Owner/Manager** (або Admin) викликає **`POST /companies/{companyId}/members/{userId}/role`** з потрібною роллю — створюється або оновлюється `CompanyMember`.
3. Учасник перевіряє **`GET /companies/{companyId}/members/me`** — бачить свою роль або **404**, якщо не доданий.
4. Далі **ті самі** `userId` + `companyId` використовуються в `Products`/`Inventory` для перевірки `ReadInternal` / `Write*`.

**Контексти:** [CompaniesContext](CompaniesContext.md), [RoleAccessMatrix](RoleAccessMatrix.md).

---

## 4. Створення товару та поява на вітрині

1. **Owner/Manager/Seller** (або Admin) викликає **`POST /companies/{companyId}/products`** — продукт зберігається з `slug`, ціною, категорією тощо.
2. Handler скидає кеш вітрини: **`catalog:products:list`** і ключ деталі за slug.
3. Публічний **`GET /catalog/products`** будує список: активні продукти + для кожного підвантажує **залишки по всіх складах** компанії і рахує **`availableQty`** та статус (`out_of_stock` / `low_stock` / `in_stock` за порогами **0 / 5**).
4. Картка **`GET /catalog/products/{slug}`** — той самий принцип наявності + кеш деталі.

**Контексти:** [ProductsContext](ProductsContext.md), [InventoryContext](InventoryContext.md), [CatalogContext](CatalogContext.md).

---

## 5. Оприбуткування та продаж зі складу

1. **Owner/Manager/Logistics** викликає **`POST .../inventory/receive`** з унікальним **`operationId`** — створюється/оновлюється `WarehouseStock`, пишеться `StockMovement`.
2. Повтор з тим самим `operationId` **відхиляється** (ідемпотентність на рівні компанії).
3. Списання — **`POST .../inventory/ship`** за тією ж схемою.
4. Після зміни кількості скидається **`catalog:products:list`**, щоб вітрина не показувала застарілий агрегат 5 хвилин.

**Контексти:** [InventoryContext](InventoryContext.md), [CatalogContext](CatalogContext.md).

---

## 6. Резерв під замовлення

1. **`POST .../inventory/reservations`** з **`reservationCode`** і **`ttlMinutes`** (1..120) збільшує `Reserved` на складі, створює запис резервації і рух типу Reserve.
2. Повторний виклик з тим самим кодом, поки резерв **активний**, повертає успіх **без подвійного списання**.
3. **`DELETE .../reservations/{reservationCode}`** звільняє резерв (логіка в `ReleaseReservationCommandHandler`).
4. Операції reserve/release пишуть події в `outbox_messages`; повторні доставки обробляються через `inbox_messages` (dedup).

**Контексти:** [InventoryContext](InventoryContext.md).

---

## 6.1 Узгодженість order-payment-inventory (outbox/inbox)

1. Критичні write-операції (`checkout`, `order status/cancel`, payment webhook/sync/jobs/refund, inventory reserve/release) додають події в `outbox_messages` з **детерміністичним** `messageId` (`DomainEventIds`).
2. `OrderMutationCoordinator` централізує cache invalidation + outbox append для order/payment змін.
3. Hangfire job `outbox-dispatch-pending` читає pending batch, застосовує retry з backoff і переводить невдалі повідомлення в poison-стан після ліміту спроб.
4. Consumer-и маркують оброблені повідомлення в `inbox_messages`; дублікати (webhook replay/retry race) стають no-op.
5. Для `orders` кешу використовується hybrid strategy: інвалідація detail + **version bump** для list scope (`my`, `company`, `admin`).
6. Inventory outbox events (`InventoryReserved`/`InventoryReleased`/`InventoryFailed`) дають safety-net для catalog cache invalidation в `OutboxEventProcessor`.
7. Кожен transition статусу order пишеться в `order_status_history` з `source` і `correlationId` для audit trail/timeline.

## 6.2 Checkout: резерв стоку та звільнення

1. **`POST /me/cart/checkout`** у межах транзакції створює order/items і викликає `CheckoutInventoryService.ReserveForOrderAsync` — для кожного line item резервується сток (`ReservationCode = order-{orderId}-product-{productId}`, `Reference = order:{orderId}`, TTL за замовчуванням 30 хв).
2. Повторний reserve з тим самим кодом (ідемпотентність) пропускається, якщо активний резерв уже існує.
3. При **скасуванні** замовлення (`POST /orders/{orderId}/cancel` + `reasonCode`/`comment`) або **failed payment** (LiqPay init fail, webhook/sync/job `Failed`) викликається `ReleaseForOrderAsync` — звільняються всі активні резерви з `Reference = order:{orderId}`.

### Cancellation policy (SLA)

| Статус | Buyer | Seller | Admin |
|--------|-------|--------|-------|
| Pending | ≤60 хв від створення | так | так |
| Paid | ≤24 год від створення | так | так |
| Processing | ні | ≤72 год | так |
| Shipped/Delivered | ні | ні | так (`FraudSuspected`/`Other`) |

Політика в `OrderCancellationPolicy`; конфіг `OrderCancellation` у appsettings.
4. Після **успішної оплати** резерв залишається до fulfillment (ship) — поточна модель.
5. Прострочені резерви знімає Hangfire `inventory-expire-reservations` через той самий `InventoryReservationReleaseService`, що й manual release.

**Контексти:** [InventoryContext](InventoryContext.md), [Orders](../Endpoints/Orders.md).

### Fulfillment (shipments)

1. Seller викликає **`POST /companies/{companyId}/orders/{orderId}/shipments`** з line items (partial ship підтримується).
2. `ShipmentFulfillmentService` агрегує `shipment_items` і при повному покритті викликає `Order.SetShipped` / `SetDelivered` без нових `OrderStatus`.
3. Backward compat: **`POST /orders/{id}/status`** з `Shipped` делегує в create-shipment для невідправлених lines.
4. Nova Poshta webhook → `ApplyCarrierEventAsync` оновлює shipment + timeline `shipping_events`.
5. Order detail містить `fulfillment` (`FulfillmentReadinessDto`).

**Документація:** [Shipping](../Endpoints/Shipping.md).

### Returns / RMA

1. Buyer **`POST /me/orders/{orderId}/returns`** (delivered + SLA window).
2. Seller approve/reject/received через company endpoints.
3. Admin **`POST /admin/returns/{returnId}/refund`** → LiqPay через `PaymentRefundExecutor`.
4. Order detail містить embedded `returns[]`.

**Документація:** [Returns](../Endpoints/Returns.md).

---

## 7. Глобальна роль адміна платформи

1. Інший Admin викликає **`PATCH /users/{id}/role`** — змінюється **глобальна** роль (`UserRole`).
2. Користувач з роллю **Admin** може заходити в **`/admin/*`** і обходити перевірки членства в **будь-якій** компанії для внутрішніх company-scoped API.

**Контексти:** [IdentityAndAccess](IdentityAndAccess.md), [RoleAccessMatrix](RoleAccessMatrix.md).

---

## 8. Web Push (користувач і адмін)

1. Клієнт (після логіну) за потреби викликає **`GET /web-push/vapid-public-key`** — отримує публічний VAPID-ключ і `subject` для `PushManager.subscribe` у Service Worker.
2. **`POST /me/web-push/subscriptions`** зберігає `endpoint` + ключі в `push_subscriptions` з прапорцями аудиторії: **користувацькі** нотифікації (`includeUserChannel`) і/або **адмінські** (`includeAdminChannel` лише якщо JWT має глобальну роль **Admin**).
3. Після успішного **`POST /me/cart/checkout`** для кожного створеного замовлення в чергу Hangfire ставиться застосункова нотифікація **`AdminNewOrder`** — отримують лише підписки з адмін-прапорцем.
4. Після **`POST /orders/{orderId}/status`** у статуси **`Shipped`** або **`Delivered`** ставиться в чергу **`UserOrderStatus`** для **покупця** (`customerId`).
5. Реальна відправка Web Push залежить від `WebPush:Enabled` і наявності VAPID-ключів; окремий канал **in-app** у джобі поки no-op (зарезервовано під майбутню таблицю нотифікацій).

**HTTP:** [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md). **Контексти:** [IdentityAndAccess](IdentityAndAccess.md), [Orders](../Endpoints/Orders.md) (checkout / статус замовлення).
