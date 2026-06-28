# PaymentsIntegrationsController — `/integrations/liqpay`

### `POST /integrations/liqpay/webhook`

- **Summary (1 рядок):** Webhook LiqPay для оновлення статусу платежів.
- **Призначення:** callback/webhook від LiqPay для зміни статусу платежів.
- **Хто може викликати:**
  - JWT: не потрібна (перевірка signature всередині)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `data` + `signature` (LiqPay)
  2. Dedup за payload/hash
  3. Оновити `payments` і пов'язані `orders`
- **Side effects (синхронно):** оновлює `payments` і пов'язані `orders`
- **Async / «магія»:** можливі Hangfire нотифікації `UserOrderStatus` / `AdminNewOrder`
- **Де на фронті:**
  - Екран: —
  - API-модуль: —
  - Статус: `planned` (server-to-server only)
- **Приймає:** body `LiqPayWebhookRequest` (`data`, `signature`).
- **Idempotency:** provider-native dedup за payload/hash (`data+signature`) без обовʼязкового `Idempotency-Key` header.
- **Config policy:** `LiqPay` keys мають приходити з secret store / environment variables для production.
- **Повертає:** **200** якщо оброблено, **401** якщо signature невалідний.
- **Metrics:** `webhook_latency_ms{provider="liqpay"}`, `webhook_operations_total`, `webhook_errors_total`.
