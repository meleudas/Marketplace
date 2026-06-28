# Observability Runbook

## Platform engineering docs

Повна інтеграція стеку (OTLP Collector, Prometheus, Jaeger, Grafana, SonarQube, NetArchTest): [docs/platform-engineering/README.md](../../../../../docs/platform-engineering/README.md).

## Telemetry export (primary)

- API експортує **metrics + traces** через **OTLP** на OpenTelemetry Collector (`OTEL_EXPORTER_OTLP_ENDPOINT`, default `http://otel-collector:4317` у Docker).
- Prometheus scrapes Collector (`:8889`), не API напряму.
- Traces → Jaeger; dashboards/alerts → Grafana (`http://localhost:3001` з profile `observability`).

Локальний запуск: `docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile observability up -d` і `OTEL_ENABLED=true` у `.env` (див. [09-local-docker-compose-runbook.md](../../../../../docs/platform-engineering/09-local-docker-compose-runbook.md)).

Перевірка стеку: `backend/scripts/observability-smoke.ps1`. Sonar: `backend/scripts/sonar-scan.ps1`. Grafana alerts: `observability/grafana/provisioning/alerting/rules.yaml` (13 правил).

## Metrics endpoint (legacy)

- `GET /metrics` — лише якщо `OpenTelemetry:EnableLegacyPrometheusEndpoint=true` (Prometheus scrape з Admin JWT; за замовчуванням **вимкнено**).
- Includes ASP.NET Core, HttpClient, runtime and custom marketplace meters.

## Key SLI

- `cache_hits_total`, `cache_misses_total`, `cache_errors_total`, `cache_latency_ms`
- `payment_operations_total`, `payment_errors_total`, `payment_latency_ms`
- `webhook_operations_total`, `webhook_errors_total`, `webhook_latency_ms`
- `hangfire_jobs_total`, `hangfire_job_errors_total`, `hangfire_job_latency_ms`
- `cart_operations_total`, `cart_errors_total`, `cart_latency_ms`
- `checkout_operations_total`, `checkout_errors_total`, `checkout_latency_ms`
- `catalog_operations_total`, `catalog_errors_total`, `catalog_latency_ms`, `catalog_search_fallback_total`
- `company_operations_total`, `company_errors_total`, `company_latency_ms`
- `favorites_operations_total`, `favorites_errors_total`, `favorites_latency_ms`
- `auth_operations_total`, `auth_errors_total`, `auth_latency_ms`
- `order_operations_total`, `order_errors_total`, `order_latency_ms`
- `product_operations_total`, `product_errors_total`, `product_latency_ms`
- `review_operations_total`, `review_errors_total`, `review_latency_ms`
- `shipping_operations_total`, `shipping_errors_total`, `shipping_latency_ms`
- `shipment_created_total`, `shipment_delivery_status_total{status}`, `shipping_webhook_events_total`
- `coupon_operations_total`, `coupon_errors_total`, `coupon_validation_failures_total`
- `hangfire_jobs_total{job="inventory-expire-reservations"}`, `hangfire_job_errors_total{job="inventory-expire-reservations"}`, `hangfire_job_latency_ms{job="inventory-expire-reservations"}`
- `notification_dispatch_total`, `notification_dispatch_errors_total`, `notification_dispatch_latency_ms`
- `notification_channel_deliveries_total`, `notification_channel_errors_total`
- `notification_dispatch_dead_letter_total{template_key}` — вичерпані retry для `notification_dispatch` (окремо від `integration_retry_dead_letter_total`)
- `outbox_dispatch_total`, `outbox_dispatch_errors_total`, `outbox_dead_letter_total`
- `integration_retry_attempts_total{kind}`, `integration_retry_dead_letter_total{kind}`
- `idempotency_begin_total`, `idempotency_conflicts_total`, `idempotency_replays_total`
- `rate_limit_rejected_total{group}` — HTTP middleware rejections (`auth`, `auth-email`, `checkout`, `review`, `payment_webhook`, `payment_admin`)
- `commission_posted_total`, `seller_ledger_entries_total{entry_type}`, `seller_payout_total{status}`, `settlement_batch_total{status}`
- `hangfire_jobs_total{job="finance-settlement-batch"}`, `hangfire_jobs_total{job="finance-seller-payout"}`

## Settlement / payout alerts

- `seller_payout_total{status="Failed"}` spike → перевірити IBAN у payout-profile, LiqPay `PayoutEnabled`, логи `finance-seller-payout`.
- `settlement_batch_total{status="Failed"}` → failed batch потребує admin `mark-paid` або повторного approve.
- `seller_ledger_entries_total` без зростання `commission_posted_total` після webhook success → перевірити `OrderFinancialsWriter` / payment completed path.
- Деталі: [Docs/Finance/Settlement.md](../Finance/Settlement.md).

- cache miss ratio spikes (`miss / (hit + miss)`) over baseline.
- webhook or payment p95 latency increase (`*_latency_ms` histogram).
- sustained growth in `*_errors_total` for payment/webhook/hangfire.
- payment admin operations latency degradation (`payment_latency_ms`) for `admin_payments_refund`, `admin_payments_sync`.
- payment admin error burst (`payment_errors_total{reason=\"unauthorized|not_found|application_failure\"}`) above baseline.
- liqpay webhook replay/signature anomalies: sustained growth of `webhook_errors_total{provider=\"liqpay\"}`.
- checkout error rate > 2% over 10m (`checkout_errors_total / checkout_operations_total`).
- cart mutation p95 latency degradation (`cart_latency_ms`) on `add_item`, `update_item_quantity`, `remove_item`, `clear_cart`.
- catalog search/list endpoints p95 latency degradation (`catalog_latency_ms`) on `search_products`, `get_products`, `get_categories`.
- unexpected growth of `catalog_search_fallback_total` (possible Elasticsearch degradation / disabled mode drift).
- company member/admin operations p95 latency degradation (`company_latency_ms`) on `admin_approve_company`, `admin_revoke_company`, `admin_set_company_commission`, `company_member_assign_role`, `company_member_change_role`.
- growth in `company_errors_total` with `reason=application_failure` for company lifecycle/member-management operations.
- favorites p95 latency degradation (`favorites_latency_ms`) on `favorites_list`, `favorites_add`, `favorites_remove`.
- favorites error burst (`favorites_errors_total`) with `reason=application_failure` або `reason=unauthorized` понад baseline.
- auth/login/register/refresh p95 latency regression (`auth_latency_ms`) for `operation=login|register|refresh`.
- auth replay/security anomalies: spike in `auth_errors_total{reason="replay_detected"}` or `auth_errors_total{reason="invalid_credentials"}`.
- sudden growth of `auth_errors_total{reason="unconfirmed_email"}` after release (email confirmation flow regression).
- orders read/write p95 latency degradation (`order_latency_ms`) for `orders_list_my`, `orders_get_my`, `orders_list_company`, `orders_get_company`, `orders_list_admin`, `orders_get_admin`, `orders_update_status`, `orders_cancel`.
- orders access anomalies: burst of `order_errors_total{reason="unauthorized|forbidden"}` above baseline.
- orders idempotency issues: growth of `order_errors_total{reason="idempotency_key_missing|idempotency_request_mismatch|idempotency_in_progress"}`.
- products moderation queue latency degradation: p95 `product_latency_ms` for `operation="admin_products_pending|admin_products_approve|admin_products_reject"` above baseline.
- products moderation failures burst: `product_errors_total{operation="admin_products_approve|admin_products_reject",reason="not_found|application_failure"}` sustained growth.
- products upload validation drift: spike in `product_errors_total{operation="products_upload_image",reason="unsupported_type|file_too_large|storage_disabled"}`.
- reviews read/write p95 latency degradation (`review_latency_ms`) for `reviews_list_product`, `reviews_create_product`, `reviews_update_product`, `reviews_delete_product`, `reviews_list_company`, `reviews_create_company`, `reviews_update_company`, `reviews_delete_company`, `reviews_reply_product`, `reviews_reply_company`, `reviews_moderate_product`, `reviews_moderate_company`.
- reviews access anomalies: burst of `review_errors_total{reason="unauthorized|forbidden"}` above baseline.
- reviews data-quality anomalies: growth of `review_errors_total{reason="not_found|application_failure"}` for moderate/update/delete operations.
- rate limit abuse spike: sustained growth of `rate_limit_rejected_total{group="auth|auth-email"}` (credential stuffing / registration spam) or `group="checkout|review"` (buyer abuse).
- inventory reservation expiry failures: `hangfire_job_errors_total{job="inventory-expire-reservations"}` > 0 for 10m.
- inventory reservation expiry latency degradation: p95 `hangfire_job_latency_ms{job="inventory-expire-reservations"}` above baseline.
- outbox dispatch failures: `hangfire_job_errors_total{job="outbox-dispatch"}` sustained growth for 10m.
- outbox DLQ growth: `outbox_dead_letter_total{category="permanent|exhausted"}` above baseline.
- integration retry DLQ: `integration_retry_dead_letter_total{kind}` (kinds: `payment_sync`, `inventory_expire`, `notification_dispatch`).
- stuck outbox: admin `GET /admin/outbox/stuck` або SQL count where `processed_at IS NULL AND dead_lettered_at IS NULL AND next_attempt_at < now()-15m AND attempts > 0`.
- outbox transient retry storm: `outbox_dispatch_errors_total{category="transient"}` burst over baseline.
- idempotency anomalies: growth of `idempotency_conflicts_total{reason="request_mismatch|in_progress"}` above baseline.
- high replay ratio in idempotent endpoints: `idempotency_replays_total` sharp increase post-release.
- payments sync job failures: `hangfire_job_errors_total{job="payments-sync-pending"}` > 0 for 10m.
- notifications dispatch error burst: `notification_dispatch_errors_total` growth over baseline for 10m.
- notifications dispatch latency degradation: p95 `notification_dispatch_latency_ms` above baseline.
- channel-level failures: `notification_channel_errors_total{channel="Push|Email|Telegram|Sms|InApp"}` sustained growth.
- delivery health drift: ratio of `notification_channel_deliveries_total{status="failed"}` to total deliveries above baseline (email/telegram/sms channels).
- notification dispatch DLQ: `notification_dispatch_dead_letter_total` spike після вичерпання `IntegrationRetry:MaxAttempts` для `notification_dispatch`.
- **Retry queue:** `integration_retry` Hangfire job (`IntegrationRetryJobs.DispatchDueAsync`) перевідправляє `notification_dispatch` через `IAppNotificationRedispatcher` → `AppNotificationJobs.DispatchAsync`. Payload містить `templateKey`, `correlationId`, `channels`, `audience`, `targetUserId`, `targetCompanyId`, `payloadJson`.
- **SMS pilot:** `AppNotifications:SmsEnabled` (default `false`); канал `Sms` у `UserOrderStatus` для `Shipped`/`Delivered`.
- shipping quote/webhook failures: growth of `shipping_errors_total{operation="quote|novaposhta_webhook|sync_status"}`.
- coupons validation/apply failures: growth of `coupon_errors_total{operation="coupon_validate|coupon_apply|coupon_remove"}`.
- coupons brute-force signal: spike in `coupon_validation_failures_total`.
- reports queue backlog growth: sustained `report_queue_backlog` above baseline.
- reports SLA breach spike: sustained growth in `report_sla_breach_total`.
- reports moderation error burst: sustained growth in `report_errors_total`.
- chat send failure spike: growth of `chat_message_errors_total`.
- chat spam attack: spike in `chat_spam_block_total`.
- chat unread backlog: sustained growth in `chat_unread_backlog`.

## Reports incident runbook

1. Перевірити feature flags:
   - `Reports:PublicCreateEnabled`
   - `Reports:ModerationEnabled`
2. Якщо є spam-wave:
   - тимчасово вимкнути `Reports:PublicCreateEnabled=false`,
   - збільшити `Reports:DuplicateCooldownMinutes`.
3. Якщо backlog росте:
   - виконати `GET /admin/reports/queue`,
   - застосувати `POST /admin/reports/bulk-actions` для batch triage.
4. Якщо SLA breach spike:
   - ескалювати кейси через `POST /admin/reports/{id}/escalate`,
   - перевірити стабілізацію `report_sla_breach_total` за 15-30 хв.

## Behavior/Analytics runbook

1. Перевірити flags:
   - `BehaviorAnalytics:BehaviorTrackingEnabled`
   - `BehaviorAnalytics:AdminAnalyticsReadEnabled`
2. Якщо ingest drop-rate росте:
   - перевірити `analytics_events_dropped_total`,
   - тимчасово зменшити навантаження через `SamplingPercent`.
3. Якщо KPI відстають:
   - запустити backfill/re-aggregation job (`behavior-aggregate-daily`),
   - перевірити `analytics_pipeline_latency_ms` і `analytics_aggregation_failures_total`.
4. Для відновлення вікна:
   - replay з `behavior_events_raw` у агрегати за потрібний період.

## Chats incident runbook

1. Перевірити feature flags:
   - `Chats:Enabled`
   - `Chats:ModerationEnabled`
   - `Chats:RealtimeEnabled`
2. Spam attack mode:
   - зменшити `Chats:MessagesPerMinute`,
   - збільшити `Chats:DuplicateWindowSeconds`,
   - увімкнути `Chats:RejectOnProhibitedContent=true` та доповнити `ProhibitedPatterns`.
3. Realtime degradation:
   - вимкнути `Chats:RealtimeEnabled=false` (fallback на HTTP polling `GET /me/chats/{id}/messages`).
4. Moderation queue lag:
   - перевірити `chat_message_errors_total` та audit у `chat_moderation_actions`,
   - застосувати `POST /admin/chats/{chatId}/moderate` для block/hide.

## Support / Helpdesk incident runbook

1. Перевірити feature flags:
   - `Support:Enabled`
   - `Support:HelpdeskSyncEnabled`
   - `Support:HelpdeskWebhookEnabled`
2. Helpdesk outage (outbound):
   - тимчасово `HelpdeskSyncEnabled=false` (internal tickets працюють),
   - моніторити `support_helpdesk_sync_failures_total` та outbox DLQ (`SupportTicket*` events),
   - після відновлення — replay DLQ через admin outbox або дочекатися `support-helpdesk-reconcile`.
3. Bad inbound webhook traffic:
   - вимкнути `HelpdeskWebhookEnabled=false`,
   - перевірити `WebhookSigningSecret`, rotate secret у провайдері.
4. SLA pressure:
   - `support_sla_breach_total`, ескалація через `POST /admin/support/tickets/{id}/escalate`,
   - tune `SlaHoursP1` / `SlaHoursP2`.
5. Manual reconciliation:
   - Hangfire job `support-helpdesk-reconcile` (hourly) порівнює `support_external_links` з `IHelpdeskPort.FetchTicketSnapshotAsync`.

## Shipping degradation runbook (Nova Poshta)

1. Перевірити прапори:
   - `Shipping:Enabled`
   - `Shipping:NovaPoshtaEnabled`
   - `NovaPoshta:Enabled`
2. Якщо провайдер деградує, перевести сервіс у fallback:
   - `NovaPoshta:Enabled=false` (quote переходить на `FallbackFlatRate`).
3. Перевірити, що `shipping_errors_total` не росте після fallback.
4. Після відновлення провайдера:
   - повернути `NovaPoshta:Enabled=true`,
   - моніторити `shipping_latency_ms` та webhook dedup події протягом 15-30 хв.

## Coupons emergency runbook

1. Для швидкого відключення checkout consume:
   - `Coupons:CheckoutConsumeEnabled=false`.
2. Для повного read/apply shutdown:
   - `Coupons:ReadEnabled=false`.
3. Для точкового інциденту з конкретним кодом:
   - викликати `POST /admin/coupons/{id}/deactivate`.
4. Після інциденту:
   - звірити `GET /admin/coupons/{id}/usage` з `coupon_usages`,
   - перевірити стабілізацію `coupon_errors_total` і `coupon_validation_failures_total`.

## Rate limiting incident runbook

1. Перевірити `rate_limit_rejected_total{group}` у Grafana — який endpoint group росте.
2. Для легітимного spike (маркетинг, flash sale):
   - тимчасово підняти `RateLimiting:Checkout:PermitLimit` або `RateLimiting:Auth:PermitLimit` у appsettings,
   - переконатися, що `ConnectionStrings:Redis` заданий у multi-instance prod (інакше ліміти per-node).
3. Для abuse (auth brute-force, webhook flood):
   - залишити або знизити ліміти; перевірити WAF / IP block на ingress,
   - `auth-email` group захищає login/register per-email (5/min default).
4. Вимкнення (лише emergency): `RateLimiting:Enabled=false` — не замінює Identity lockout.

## Health / readiness runbook

| Probe | Endpoint | Required for ready | Degraded meaning |
|-------|----------|-------------------|------------------|
| `postgres` | `/health/ready` | **так** (503 якщо down) | — |
| `redis` | `/health/ready` | ні | cache/rate-limit fallback на memory per instance |
| `elasticsearch` | `/health/ready` | ні | catalog search fallback (`catalog_search_fallback_total`) |
| `storage` | `/health/ready` | ні (якщо `Storage:Enabled=false` — healthy skip) | uploads/media degraded |
| `queue` | `/health/ready` | ні (warning only v1) | outbox backlog `pending>500` або oldest age >30 min |

**Дії:**
1. `GET /health/ready` — якщо `503`, перевірити `checks.postgres` (DB connectivity, migrations).
2. Redis degraded — перевірити `ConnectionStrings:Redis`, redeploy після відновлення; моніторити cache miss ratio.
3. Elasticsearch degraded — `Elasticsearch:Enabled=false` тимчасово або відновити кластер; перевірити `catalog_search_fallback_total`.
4. Storage degraded — MinIO/S3 credentials, `Storage:Endpoint`, bucket policy.
5. Queue degraded — `GET /admin/outbox` stats, DLQ replay; перевірити Hangfire worker + `outbox_dispatch_errors_total`.
6. Liveness (`/health`, `/health/live`) лише для restart pod — не використовувати для traffic routing.
