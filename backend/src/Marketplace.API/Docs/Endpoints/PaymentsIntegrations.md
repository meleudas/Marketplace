# PaymentsIntegrationsController — `/integrations/liqpay`

### `POST /integrations/liqpay/webhook`

- **Призначення:** callback/webhook від LiqPay для зміни статусу платежів.
- **Приймає:** body `LiqPayWebhookRequest` (`data`, `signature`).
- **Авторизація:** `AllowAnonymous` (перевірка signature всередині).
- **Повертає:** **200** якщо оброблено, **401** якщо signature невалідний.
- **Side effects:** оновлює `payments` і пов'язані `orders`.
