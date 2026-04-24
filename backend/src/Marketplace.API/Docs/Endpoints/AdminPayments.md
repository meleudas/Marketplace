# AdminPaymentsController — `/admin/payments`

Усі маршрути: **`[Authorize(Roles = "Admin")]`**.

### `POST /admin/payments/{paymentId}/refund`

- **Призначення:** виконати refund через LiqPay для оплаченого платежу.
- **Приймає:** path `paymentId` + body `RequestRefundBody`.
- **Повертає:** **200** порожнє тіло.
- **Помилки:** **404** `Payment not found`, **400** якщо платіж не в статусі `completed`.

### `POST /admin/payments/{paymentId}/sync`

- **Призначення:** ручна синхронізація статусу платежу з LiqPay.
- **Приймає:** path `paymentId`.
- **Повертає:** **200** порожнє тіло.
- **Side effects:** оновлює статус `payments` і пов'язаного `orders`.
