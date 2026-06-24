# Returns Endpoints

## `POST /me/orders/{orderId}/returns`

- **Summary:** Buyer створює RMA для доставленого замовлення.
- **Auth:** JWT (buyer = власник замовлення).
- **Body:** `reasonCode`, `comment?`, `lines[]` (`orderItemId`, `quantity`, `reason?`).
- **Idempotency-Key:** обов'язковий.
- **Повертає:** `ReturnRequestDetailDto`.

## `GET /me/returns`

- **Summary:** Список RMA поточного покупця.
- **Повертає:** `ReturnRequestSummaryDto[]`.

## `GET /me/returns/{returnId}`

- **Summary:** Деталі RMA для покупця.
- **Повертає:** `ReturnRequestDetailDto`.

## `GET /companies/{companyId}/returns`

- **Summary:** Черга RMA продавця (опційний фільтр `status`).
- **Auth:** член компанії або Admin.

## `GET /companies/{companyId}/returns/{returnId}`

- **Summary:** Деталі RMA для продавця (член компанії).
- **Auth:** член компанії або Admin.
- **Повертає:** `ReturnRequestDetailDto`.

## `POST /companies/{companyId}/returns/{returnId}/approve`

- **Summary:** Схвалити RMA.
- **Idempotency-Key:** обов'язковий.

## `POST /companies/{companyId}/returns/{returnId}/reject`

- **Summary:** Відхилити RMA з `reason`.
- **Idempotency-Key:** обов'язковий.

## `POST /companies/{companyId}/returns/{returnId}/received`

- **Summary:** Позначити товар отриманим на складі.
- **Idempotency-Key:** обов'язковий.

## `POST /admin/returns/{returnId}/refund`

- **Summary:** Admin refund через LiqPay після `Received` (опційний partial `amount`).
- **Idempotency-Key:** обов'язковий.

## Order detail

`GET /me/orders/{id}` та company/admin variants містять embedded `returns[]` та `fulfillment` (`FulfillmentReadinessDto`).

## Статуси RMA

`Requested` → `Approved` / `Rejected` → `Received` → `Refunded`.

## Конфігурація

`ReturnRequests` у `appsettings.json`: `MaxDaysAfterDelivery`, `AllowReturnWhileShipped`, `RestockOnReceive`.
