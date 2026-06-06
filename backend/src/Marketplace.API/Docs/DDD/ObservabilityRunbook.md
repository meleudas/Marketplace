# Observability Runbook

## Platform engineering docs

Повна інтеграція стеку (OTLP Collector, Prometheus, Jaeger, Grafana, SonarQube, NetArchTest): [docs/platform-engineering/README.md](../../../../../docs/platform-engineering/README.md).

## Telemetry export (primary)

- API експортує **metrics + traces** через **OTLP** на OpenTelemetry Collector (`OTEL_EXPORTER_OTLP_ENDPOINT`, default `http://otel-collector:4317` у Docker).
- Prometheus scrapes Collector (`:8889`), не API напряму.
- Traces → Jaeger; dashboards/alerts → Grafana (`http://localhost:3001` з profile `observability`).

Локальний запуск: `docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile observability up -d` і `OTEL_ENABLED=true` у `.env` (див. [09-local-docker-compose-runbook.md](../../../../../docs/platform-engineering/09-local-docker-compose-runbook.md)).

Перевірка стеку: `backend/scripts/observability-smoke.ps1`. Sonar: `backend/scripts/sonar-scan.ps1`. Grafana alerts: `observability/grafana/provisioning/alerting/rules.yaml` (11 правил).

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
- `coupon_operations_total`, `coupon_errors_total`, `coupon_validation_failures_total`
- `hangfire_jobs_total{job="inventory-expire-reservations"}`, `hangfire_job_errors_total{job="inventory-expire-reservations"}`, `hangfire_job_latency_ms{job="inventory-expire-reservations"}`
- `notification_dispatch_total`, `notification_dispatch_errors_total`, `notification_dispatch_latency_ms`
- `notification_channel_deliveries_total`, `notification_channel_errors_total`
- `outbox_dispatch_total`, `outbox_dispatch_errors_total`, `outbox_dead_letter_total`
- `idempotency_begin_total`, `idempotency_conflicts_total`, `idempotency_replays_total`

## Suggested alerts

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
- inventory reservation expiry failures: `hangfire_job_errors_total{job="inventory-expire-reservations"}` > 0 for 10m.
- inventory reservation expiry latency degradation: p95 `hangfire_job_latency_ms{job="inventory-expire-reservations"}` above baseline.
- outbox dispatch failures: `hangfire_job_errors_total{job="outbox-dispatch"}` sustained growth for 10m.
- outbox DLQ growth: `outbox_dead_letter_total{category="permanent|exhausted"}` above baseline.
- outbox transient retry storm: `outbox_dispatch_errors_total{category="transient"}` burst over baseline.
- idempotency anomalies: growth of `idempotency_conflicts_total{reason="request_mismatch|in_progress"}` above baseline.
- high replay ratio in idempotent endpoints: `idempotency_replays_total` sharp increase post-release.
- payments sync job failures: `hangfire_job_errors_total{job="payments-sync-pending"}` > 0 for 10m.
- notifications dispatch error burst: `notification_dispatch_errors_total` growth over baseline for 10m.
- notifications dispatch latency degradation: p95 `notification_dispatch_latency_ms` above baseline.
- channel-level failures: `notification_channel_errors_total{channel="Push|Email|Telegram|InApp"}` sustained growth.
- delivery health drift: ratio of `notification_channel_deliveries_total{status="failed"}` to total deliveries above baseline.
- shipping quote/webhook failures: growth of `shipping_errors_total{operation="quote|novaposhta_webhook|sync_status"}`.
- coupons validation/apply failures: growth of `coupon_errors_total{operation="coupon_validate|coupon_apply|coupon_remove"}`.
- coupons brute-force signal: spike in `coupon_validation_failures_total`.
- reports queue backlog growth: sustained `report_queue_backlog` above baseline.
- reports SLA breach spike: sustained growth in `report_sla_breach_total`.
- reports moderation error burst: sustained growth in `report_errors_total`.

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
