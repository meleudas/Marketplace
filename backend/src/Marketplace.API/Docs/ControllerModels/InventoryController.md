# InventoryController

Контролер: `/companies/{companyId}/*`

## Авторизація
- Увесь контролер: `Authorize`
- Права в бізнес-логіці:
  - write inventory: `Owner|Manager|Logistics|Admin`
  - read internal: будь-яка company-role + `Admin`

## Моделі запитів (copy-paste)

### GET `/companies/{companyId}/warehouses`
- Body не потрібен.

### POST `/companies/{companyId}/warehouses`
```json
{
  "name": "Main Warehouse",
  "code": "MAIN",
  "street": "Warehouse st 1",
  "city": "Kyiv",
  "state": "Kyiv",
  "postalCode": "02000",
  "country": "UA",
  "timeZone": "Europe/Kyiv",
  "priority": 0
}
```

### PUT `/companies/{companyId}/warehouses/{warehouseId}`
```json
{
  "name": "Main Warehouse Updated",
  "code": "MAIN",
  "street": "Warehouse st 2",
  "city": "Kyiv",
  "state": "Kyiv",
  "postalCode": "02000",
  "country": "UA",
  "timeZone": "Europe/Kyiv",
  "priority": 1
}
```

### POST `/companies/{companyId}/warehouses/{warehouseId}/deactivate`
- Body не потрібен.

### GET `/companies/{companyId}/inventory/stocks?warehouseId=&productId=`
- Body не потрібен.

### GET `/companies/{companyId}/inventory/movements?productId=`
- Body не потрібен.

### POST `/companies/{companyId}/inventory/receive`
```json
{
  "warehouseId": 1,
  "productId": 1001,
  "quantity": 20,
  "operationId": "receive-1001-20260408-1",
  "reference": "PO-123"
}
```

### POST `/companies/{companyId}/inventory/ship`
```json
{
  "warehouseId": 1,
  "productId": 1001,
  "quantity": 3,
  "operationId": "ship-1001-20260408-1",
  "reference": "ORDER-501"
}
```

### POST `/companies/{companyId}/inventory/adjust`
```json
{
  "warehouseId": 1,
  "productId": 1001,
  "onHand": 25,
  "reserved": 2,
  "reorderPoint": 5,
  "operationId": "adjust-1001-20260408-1",
  "reason": "Inventory count correction"
}
```

### POST `/companies/{companyId}/inventory/transfer`
```json
{
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "productId": 1001,
  "quantity": 4,
  "operationId": "transfer-1001-20260408-1"
}
```

### POST `/companies/{companyId}/inventory/reservations`
```json
{
  "warehouseId": 1,
  "productId": 1001,
  "quantity": 2,
  "reservationCode": "RES-ORDER-501",
  "ttlMinutes": 15,
  "reference": "ORDER-501"
}
```

### DELETE `/companies/{companyId}/inventory/reservations/{reservationCode}`
- Body не потрібен.
