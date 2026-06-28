# InventoryController — `/companies/{companyId}`

Усі маршрути: **`[Authorize]`** + Bearer.

### Матриця доступу (з `InventoryAccessService`)

| Дія | Умова |
|-----|--------|
| Будь-який запит | Член компанії **або** **Admin** |
| Читання (`ReadInternal`) | Будь-який член |
| Операції запису (`WriteStock`) | **Owner**, **Manager**, **Logistics** |
| Admin | Bypass |

Операції **`WriteStock`**: створення/оновлення/деактивація складу, receive/ship/adjust/transfer, reserve, release reservation.

Після **receive**, **release reservation**, **adjust** та **transfer** (оновлення залишків) бекенд може поставити в чергу застосункові нотифікації «товар знову в наявності» для користувачів з `cart_stock_watches` (див. [EventCatalog.md](../Notifications/EventCatalog.md)); це **не** auth-черга `INotificationDispatcher`, а `IAppNotificationScheduler`.

---

## Склади

### `GET /companies/{companyId}/warehouses`

- **Summary (1 рядок):** Список складів компанії.
- **Призначення:** отримати всі склади компанії для внутрішнього перегляду.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: будь-який член (`ReadInternal`)
- **Бізнес-логіка:**
  1. Перевірити членство або Admin
  2. Завантажити склади компанії
  3. Повернути список `WarehouseDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Повертає:** список складів (`WarehouseDto` тощо — з `GetCompanyWarehousesQuery`).

### `POST /companies/{companyId}/warehouses`

- **Summary (1 рядок):** Створення нового складу.
- **Призначення:** додати склад до компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити `WriteStock` доступ
  2. Валідувати `CreateWarehouseRequest`
  3. Створити запис складу
- **Side effects (синхронно):** новий запис складу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory (create warehouse, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** body `CreateWarehouseRequest` (`name`, `code`, адреса: `street`, `city`, `state`, `postalCode`, `country`, `timeZone`, `priority`).
- **Повертає:** створений склад (тип з handler-а).

### `PUT /companies/{companyId}/warehouses/{warehouseId}`

- **Summary (1 рядок):** Оновлення реквізитів складу.
- **Призначення:** змінити дані існуючого складу.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити `WriteStock` доступ
  2. Знайти склад за `warehouseId`
  3. Застосувати оновлення
- **Side effects (синхронно):** оновлення реквізитів складу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory (edit warehouse, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** той самий body, що й create.

### `POST /companies/{companyId}/warehouses/{warehouseId}/deactivate`

- **Summary (1 рядок):** Деактивація складу.
- **Призначення:** деактивувати склад (`warehouse.Deactivate()`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити `WriteStock` доступ
  2. Знайти склад і викликати `Deactivate()`
- **Side effects (синхронно):** склад деактивовано; **кеш каталогу в цьому handler не чіпається** (на відміну від рухів стоку)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory (planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`

---

## Залишки та журнал

### `GET /companies/{companyId}/inventory/stocks`

- **Summary (1 рядок):** Залишки на складах з опційними фільтрами.
- **Призначення:** отримати залишки товарів на складах компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: будь-який член (`ReadInternal`)
- **Бізнес-логіка:**
  1. Перевірити `ReadInternal` доступ
  2. Застосувати фільтри `warehouseId?`, `productId?`
  3. Повернути залишки
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** query `warehouseId?`, `productId?` (long).
- **Повертає:** залишки (`GetWarehouseStockQuery`).

### `GET /companies/{companyId}/inventory/movements`

- **Summary (1 рядок):** Журнал рухів стоку.
- **Призначення:** історія операцій зі стоком компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: будь-який член (`ReadInternal`)
- **Бізнес-логіка:**
  1. Перевірити `ReadInternal` доступ
  2. Завантажити рухи з опційним фільтром `productId`
  3. Повернути журнал
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory / movements (planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** query `productId?`.
- **Повертає:** журнал рухів (`GetStockMovementsQuery`).

---

## Операції зі стоком

### `POST /companies/{companyId}/inventory/receive`

- **Summary (1 рядок):** Прийом товару на склад (збільшення on-hand).
- **Призначення:** збільшити залишок товару на складі.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити ідемпотентність по `operationId`
  2. Збільшити on-hand на складі
  3. Записати `StockMovement`; інвалідувати `catalog:products:list`
- **Side effects (синхронно):** збільшення on-hand; запис movement; інвалідація `catalog:products:list`
- **Async / «магія»:** можлива нотифікація «restock» для `cart_stock_watches` через `IAppNotificationScheduler`
- **Де на фронті:**
  - Екран: Workspace inventory (receive, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** body `StockOperationRequest` (`warehouseId`, `productId`, `quantity`, `operationId`, `reference?`).
- **Повертає:** оновлений/створений рядок стоку як `WarehouseStockDto`.
- **Idempotency:** якщо `operationId` вже є в `StockMovement` для компанії → помилка **`Operation already processed`**.

### `POST /companies/{companyId}/inventory/ship`

- **Summary (1 рядок):** Відвантаження/списання товару зі складу.
- **Призначення:** відвантаження/списання.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити ідемпотентність по `operationId`
  2. Зменшити доступний сток
  3. Записати movement; інвалідувати кеш каталогу
- **Side effects (синхронно):** зменшення доступного стоку; movement; інвалідація списку каталогу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory (ship, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** `StockOperationRequest`.
- **Idempotency:** та сама **ідемпотентність** по `operationId`.

### `POST /companies/{companyId}/inventory/adjust`

- **Summary (1 рядок):** Коригування абсолютних значень залишку.
- **Призначення:** виставити абсолютні значення on-hand, reserved, reorderPoint.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити ідемпотентність по `operationId`
  2. Виставити абсолютні значення залишку (логіка entity)
  3. Записати movement; інвалідувати кеш каталогу
- **Side effects (синхронно):** виставлення абсолютних значень; movement; інвалідація списку каталогу
- **Async / «магія»:** можлива нотифікація «restock» для `cart_stock_watches`
- **Де на фронті:**
  - Екран: Workspace inventory (adjust, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** body `AdjustStockRequest` (`warehouseId`, `productId`, `onHand`, `reserved`, `reorderPoint`, `operationId`, `reason`).
- **Idempotency:** **ідемпотентність** по `operationId`.

### `POST /companies/{companyId}/inventory/transfer`

- **Summary (1 рядок):** Переміщення стоку між складами.
- **Призначення:** зменшити на джерелі, збільшити на цілі.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Перевірити ідемпотентність по `operationId`
  2. Зменшити сток на `fromWarehouseId`, збільшити на `toWarehouseId`
  3. Записати movements (transfer out/in); інвалідувати кеш
- **Side effects (синхронно):** movements; інвалідація списку каталогу
- **Async / «магія»:** можлива нотифікація «restock» для `cart_stock_watches`
- **Де на фронті:**
  - Екран: Workspace inventory (transfer, planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** body `TransferStockRequest` (`fromWarehouseId`, `toWarehouseId`, `productId`, `quantity`, `operationId`).
- **Idempotency:** **ідемпотентність** по `operationId`.

### `POST /companies/{companyId}/inventory/reservations`

- **Summary (1 рядок):** Резервування стоку з TTL.
- **Призначення:** зарезервувати кількість товару на складі.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Якщо активна резервація з тим самим `reservationCode` — успіх без змін (ідемпотентно)
  2. Інакше: `stock.Reserve(quantity)`; нова `InventoryReservation` з TTL
  3. Записати movement Reserve; інвалідувати кеш
- **Side effects (синхронно):** резервування стоку; movement; інвалідація `catalog:products:list`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace inventory / checkout integration (planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Приймає:** body `ReserveStockRequest` (`warehouseId`, `productId`, `quantity`, `reservationCode`, `ttlMinutes`, `reference?`).
- **Idempotency:** якщо вже є **активна** резервація з тим самим `reservationCode` → **успіх без повторних змін**.
- **Помилки:** `Stock not found`, `Forbidden`.

### `DELETE /companies/{companyId}/inventory/reservations/{reservationCode}`

- **Summary (1 рядок):** Зняття резерву стоку.
- **Призначення:** зняти резерв (release).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Logistics** (`WriteStock`)
- **Бізнес-логіка:**
  1. Знайти резервацію за `reservationCode`
  2. Зняти резерв і оновити сток
  3. Записати movement; інвалідувати кеш
- **Side effects (синхронно):** оновлення стоку/резервації; movement; інвалідація списку каталогу
- **Async / «магія»:** можлива нотифікація «restock» для `cart_stock_watches`
- **Де на фронті:**
  - Екран: Workspace inventory (planned)
  - API-модуль: `frontend/src/features/workspace/api/inventory.api.ts`
  - Статус: `partial`
- **Помилки:** невідомий код / не та компанія — згідно handler.

---

## Типові помилки

- `Forbidden` — немає членства або роль без `WriteStock` для мутацій.
- `Operation already processed` — повтор тієї ж операції з тим самим `operationId` (де перевіряється).
- `Stock not found` / `Warehouse not found` — невірні id або чужа компанія.
