# External Integrations Readiness

- Статус реалізації: `Implemented` (з sandbox/fallback режимами)
- Готовність: **87/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- LiqPay: `Suite=Payments` gate + LiqPayWebhookPostgresTests + E2E webhook
- Nova Poshta: NovaPoshtaWebhookPostgresTests, shipping-gate CI
- Google OAuth: ExternalAuthCallbackPostgresTests (fake OAuth port)
- Config template: [backend/.env.example](../../backend/.env.example)

## Інтеграції

| Інтеграція | Реалізація | Тести | Prod readiness | Score |
|------------|------------|-------|----------------|------:|
| LiqPay payments | Webhook HMAC, idempotency, sync jobs | U/L/C/E | Потрібні prod keys у secrets | 86 |
| Nova Poshta shipping | Quotes, webhook dedup | U/L/C | `NOVAPOSHTA__ENABLED=false` за замовч. | 80 |
| Google OAuth | Callback, user provision | C | Потрібні CLIENT_ID/SECRET | 82 |
| Telegram | Webhook secret, 2FA, link codes | E (webhook) | BOTTOKEN у secrets | 78 |
| SendGrid email | AppNotifications scheduler | Unit mocks | API key у secrets | 75 |
| Web Push (VAPID) | PushSubscription persistence | C + E | `WEBPUSH__ENABLED=false` за замовч. | 79 |
| ML Recommendations | Train→MinIO→promote→inference | C | Fallback при відсутності моделі | 84 |

## Що готово

- Webhook endpoints з перевіркою підпису/секрету (LiqPay, Telegram, Nova Poshta).
- Feature flags для shipping, coupons, web push — безпечний default «вимкнено» для зовнішніх API.
- Recommendation pipeline з fallback (`UsedFallback=true`) коли активної моделі немає.
- `.env.example` документує всі зовнішні ключі без реальних значень.

## Blockers (P0)

- ~~Production secrets~~ **CLOSED** — validator + policy doc

## Near-term (P2)

- Документувати rotation policy для webhook secrets.

## Optional (P2)

- Circuit breaker / timeout dashboards per integration.
- Synthetic probes для LiqPay callback URL з prod.

## Checklist

- [x] Webhook signature validation
- [x] Idempotency на payment webhooks
- [x] Graceful degradation (ML fallback, shipping flags)
- [x] Container tests для критичних інтеграцій
- [ ] Prod secrets automation (Vault/K8s secrets)
- [ ] Staging E2E з реальними sandbox провайдерами
