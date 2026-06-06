# Domain Report: Payments

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Payments`
- `backend/src/Marketplace.Application/Payments`
- `backend/src/Marketplace.API/Controllers/PaymentsIntegrationsController.cs`
- `backend/src/Marketplace.API/Controllers/AdminPaymentsController.cs`
- `backend/src/Marketplace.Infrastructure/Jobs/PaymentJobs.cs`

## Що вже готово

- Provider-native idempotency для LiqPay webhook без mandatory `Idempotency-Key` header (dedup за payload/hash + inbox).
- Додано monotonic guard у sync flow, щоб виключити status downgrade.
- Розширено test contour: `application + API + SQLite integration + contract + security + performance` для payments сценаріїв.
- Уніфіковано `Suite=Payments` для профільних тестів та додано окремий `payments-gate` (coverage threshold 12%).
- Підсилено observability для admin payments operations (`payment_operations_total`, `payment_errors_total`, `payment_latency_ms`) і оновлено endpoint/runbook docs.

## Blockers

Blockers закриті.

## Near-term

- Тримати `payments-gate` не нижче coverage threshold 12% при зміні webhook/refund logic.
- Додати dashboard для `payment_errors_total` і `webhook_errors_total` з розрізом reason/status.

## Optional

- Підготувати абстракцію під multi-provider payments.
- Додати payment reconciliation report.

## Мінімальний checklist

- [x] Webhook верифікує підпис і не приймає replay.
- [x] Refund кейси покриті включно з помилками провайдера.
- [x] Admin payments API має authz coverage.
- [x] Прод-конфіг LiqPay URL/секретів винесений в secret store.
