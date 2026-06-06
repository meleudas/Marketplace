# PaymentsIntegrationsController — `/integrations/liqpay`

### `POST /integrations/liqpay/webhook`

- **Призначення:** callback/webhook від LiqPay для зміни статусу платежів.
- **Приймає:** body `LiqPayWebhookRequest` (`data`, `signature`).
- **Авторизація:** `AllowAnonymous` (перевірка signature всередині).
- **Idempotency:** provider-native dedup за payload/hash (`data+signature`) без обовʼязкового `Idempotency-Key` header.
- **Config policy:** `LiqPay` keys мають приходити з secret store / environment variables для production.
- **Повертає:** **200** якщо оброблено, **401** якщо signature невалідний.
- **Side effects:** оновлює `payments` і пов'язані `orders`.
- **Metrics:** `webhook_latency_ms{provider="liqpay"}`, `webhook_operations_total`, `webhook_errors_total`.
