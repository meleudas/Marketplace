# Bounded context: Inventory (склади та рух товару)

## Призначення

Операційний облік: **склади**, **залишки** по парі (склад × товар), **рухи** (audit trail), **резервації** з TTL.

## Ключові сутності

- **Warehouse** — належить `CompanyId`, має прапор активності.
- **WarehouseStock** — `OnHand`, `Reserved`, `ReorderPoint`; **Available** використовується для вітрини (зазвичай `OnHand - Reserved` у домені — уточнюйте entity).
- **StockMovement** — тип руху (receive, ship, adjust, transfer, reserve, …), кількість, посилання, актор.
- **InventoryReservation** — код резервації, термін дії, статус.

## Доступ

`InventoryAccessService`: читання — будь-який член; запис стоку — **Owner / Manager / Logistics**; **Admin** — bypass.

## Ідемпотентність

- Для **receive / ship / adjust / transfer** повтор з тим самим **`operationId`** у межах компанії відсікається (`ExistsByOperationIdAsync`) → помилка **`Operation already processed`**.
- Для **reserve** якщо активна резервація з тим самим **`reservationCode`** вже є — повтор повертає **успіх без змін**.

## TTL резервації

`ttlMinutes` **обмежується** до діапазону **1..120** хвилин у handler-і резерву.

## Зв’язки з каталогом

Будь-яка операція, що змінює відображувану наявність, **скидає** кеш `catalog:products:list` (деталь може лишатись до TTL або скидатись окремо — див. конкретний handler).

## HTTP API

[Endpoints/Inventory.md](../Endpoints/Inventory.md)

## Код

- `Marketplace.API/Controllers/InventoryController.cs`
- `Marketplace.Application/Inventory/**`
- `Marketplace.Domain/Inventory/**`
