# AdminPaymentsController — `/admin/payments`

Усі маршрути: **`[Authorize(Roles = "Admin")]`**.

### `POST /admin/payments/{paymentId}/refund`

- **Summary (1 рядок):** Повернення коштів через LiqPay.
- **Призначення:** виконати refund через LiqPay для оплаченого платежу.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти платіж за `paymentId`
  2. Перевірити статус `completed`
  3. Викликати LiqPay refund API
- **Side effects (синхронно):** оновлення статусу платежу
- **Async / «магія»:** LiqPay provider callback може оновити фінальний статус
- **Де на фронті:**
  - Екран: AdminShell / payments (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `paymentId` + body `RequestRefundBody`.
- **Повертає:** **200** порожнє тіло.
- **Помилки:** **404** `Payment not found`, **400** якщо платіж не в статусі `completed`.
- **Metrics:** `payment_latency_ms{operation="admin_payments_refund"}`, `payment_operations_total`, `payment_errors_total{reason="unauthorized|not_found|application_failure"}`.

### `POST /admin/payments/{paymentId}/sync`

- **Summary (1 рядок):** Ручна синхронізація статусу платежу з LiqPay.
- **Призначення:** ручна синхронізація статусу платежу з LiqPay.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти платіж за `paymentId`
  2. Запитати актуальний статус у LiqPay
  3. Оновити `payments` і пов'язаний `orders`
- **Side effects (синхронно):** оновлює статус `payments` і пов'язаного `orders`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / payments sync (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `paymentId`.
- **Повертає:** **200** порожнє тіло.
- **Metrics:** `payment_latency_ms{operation="admin_payments_sync"}`, `payment_operations_total`, `payment_errors_total{reason="unauthorized|not_found|application_failure"}`.
