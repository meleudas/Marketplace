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

---

## Склади

### `GET /companies/{companyId}/warehouses`

- **Повертає:** список складів (`WarehouseDto` тощо — з `GetCompanyWarehousesQuery`).
- **Авторизація:** `ReadInternal`.

### `POST /companies/{companyId}/warehouses`

- **Приймає:** body `CreateWarehouseRequest` (`name`, `code`, адреса: `street`, `city`, `state`, `postalCode`, `country`, `timeZone`, `priority`).
- **Повертає:** створений склад (тип з handler-а).
- **Авторизація:** `WriteStock`.
- **Side effects:** новий запис складу.

### `PUT /companies/{companyId}/warehouses/{warehouseId}`

- **Приймає:** той самий body, що й create.
- **Авторизація:** `WriteStock`.
- **Side effects:** оновлення реквізитів складу.

### `POST /companies/{companyId}/warehouses/{warehouseId}/deactivate`

- **Авторизація:** `WriteStock`.
- **Side effects:** склад деактивовано (`warehouse.Deactivate()`); **кеш каталогу в цьому handler не чіпається** (на відміну від рухів стоку).

---

## Залишки та журнал

### `GET /companies/{companyId}/inventory/stocks`

- **Приймає:** query `warehouseId?`, `productId?` (long).
- **Повертає:** залишки (`GetWarehouseStockQuery`).
- **Авторизація:** `ReadInternal`.

### `GET /companies/{companyId}/inventory/movements`

- **Приймає:** query `productId?`.
- **Повертає:** журнал рухів (`GetStockMovementsQuery`).
- **Авторизація:** `ReadInternal`.

---

## Операції зі стоком

### `POST /companies/{companyId}/inventory/receive`

- **Приймає:** body `StockOperationRequest` (`warehouseId`, `productId`, `quantity`, `operationId`, `reference?`).
- **Повертає:** оновлений/створений рядок стоку як `WarehouseStockDto`.
- **Авторизація:** `WriteStock`.
- **Side effects:** збільшення on-hand; запис `StockMovement` типу прийняття; **ідемпотентність:** якщо `operationId` вже є в `StockMovement` для компанії → помилка **`Operation already processed`**; **інвалідація** `catalog:products:list`.

### `POST /companies/{companyId}/inventory/ship`

- **Призначення:** відвантаження/списання.
- **Приймає:** `StockOperationRequest`.
- **Авторизація:** `WriteStock`.
- **Side effects:** зменшення доступного стоку; movement; та сама **ідемпотентність** по `operationId`; **інвалідація** списку каталогу.

### `POST /companies/{companyId}/inventory/adjust`

- **Приймає:** body `AdjustStockRequest` (`warehouseId`, `productId`, `onHand`, `reserved`, `reorderPoint`, `operationId`, `reason`).
- **Авторизація:** `WriteStock`.
- **Side effects:** виставлення абсолютних значень залишку (логіка entity); movement; **ідемпотентність** по `operationId`; **інвалідація** списку каталогу.

### `POST /companies/{companyId}/inventory/transfer`

- **Приймає:** body `TransferStockRequest` (`fromWarehouseId`, `toWarehouseId`, `productId`, `quantity`, `operationId`).
- **Авторизація:** `WriteStock`.
- **Side effects:** зменшення на джерелі, збільшення на цілі; movements (transfer out/in); **ідемпотентність** по `operationId`; **інвалідація** списку каталогу.

### `POST /companies/{companyId}/inventory/reservations`

- **Приймає:** body `ReserveStockRequest` (`warehouseId`, `productId`, `quantity`, `reservationCode`, `ttlMinutes`, `reference?`).
- **Авторизація:** `WriteStock`.
- **Side effects:**
  - Якщо вже є **активна** резервація з тим самим `reservationCode` → **успіх без повторних змін** (ідемпотентно).
  - Інакше: `stock.Reserve(quantity)`; нова `InventoryReservation` з `ExpiresAt = UtcNow + clamp(ttlMinutes, 1..120)` хв; movement типу **Reserve**; **інвалідація** `catalog:products:list`.
- **Помилки:** `Stock not found`, `Forbidden`.

### `DELETE /companies/{companyId}/inventory/reservations/{reservationCode}`

- **Призначення:** зняти резерв (release).
- **Авторизація:** `WriteStock`.
- **Side effects:** оновлення стоку/резервації; movement; **інвалідація** списку каталогу.
- **Помилки:** невідомий код / не та компанія — згідно handler.

---

## Типові помилки

- `Forbidden` — немає членства або роль без `WriteStock` для мутацій.
- `Operation already processed` — повтор тієї ж операції з тим самим `operationId` (де перевіряється).
- `Stock not found` / `Warehouse not found` — невірні id або чужа компанія.
