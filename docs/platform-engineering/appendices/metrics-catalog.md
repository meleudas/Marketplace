# Appendix — Каталог метрик

Джерело business metrics: [`MarketplaceMetrics.cs`](../../../backend/src/Marketplace.Infrastructure/Observability/MarketplaceMetrics.cs).  
Meter: `Marketplace.Observability`.

## 1. Business custom metrics

### Cache

| Name | Type | Labels | Domain | PromQL example | Alert |
|------|------|--------|--------|----------------|-------|
| `cache_hits_total` | Counter | — | Platform | `rate(cache_hits_total[5m])` | miss ratio |
| `cache_misses_total` | Counter | — | Platform | `rate(cache_misses_total[5m])` | yes |
| `cache_errors_total` | Counter | — | Platform | `rate(cache_errors_total[10m])` | yes |
| `cache_latency_ms` | Histogram | — | Platform | `histogram_quantile(0.95, rate(cache_latency_ms_bucket[5m]))` | yes |

### Payments & webhooks

| Name | Type | Labels | Domain | PromQL | Alert |
|------|------|--------|--------|--------|-------|
| `payment_operations_total` | Counter | `operation` | Payments | `sum by (operation)(rate(...))` | yes |
| `payment_errors_total` | Counter | `operation`, `reason` | Payments | error rate | yes |
| `payment_latency_ms` | Histogram | `operation` | Payments | p95 | yes |
| `webhook_operations_total` | Counter | `provider`, `operation` | Payments | rate | yes |
| `webhook_errors_total` | Counter | `provider`, `reason` | Payments | rate | yes |
| `webhook_latency_ms` | Histogram | `provider` | Payments | p95 | yes |

### Hangfire

| Name | Type | Labels | Domain | PromQL | Alert |
|------|------|--------|--------|--------|-------|
| `hangfire_jobs_total` | Counter | `job` | Platform | `rate(...{job="outbox-dispatch-pending"})` | yes |
| `hangfire_job_errors_total` | Counter | `job` | Platform | `> 0 for 10m` | yes |
| `hangfire_job_latency_ms` | Histogram | `job` | Platform | p95 | yes |

Jobs з `Program.cs`: `inventory-expire-reservations`, `search-full-reindex-products`, `payments-sync-pending-liqpay`, `outbox-dispatch-pending`, `media-cleanup-orphans`, `app-notifications-prune-expired-inapp`.

### Cart & checkout

| Name | Type | Labels | Domain | Alert |
|------|------|--------|--------|-------|
| `cart_operations_total` | Counter | `operation` | Cart | yes |
| `cart_errors_total` | Counter | `operation`, `reason` | Cart | yes |
| `cart_latency_ms` | Histogram | `operation` | Cart | yes |
| `checkout_operations_total` | Counter | `operation` | Cart | yes |
| `checkout_errors_total` | Counter | `operation`, `reason` | Cart | yes |
| `checkout_latency_ms` | Histogram | `operation` | Cart | yes |

### Catalog

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `catalog_operations_total` | Counter | `operation` | yes |
| `catalog_errors_total` | Counter | `operation`, `reason` | yes |
| `catalog_latency_ms` | Histogram | `operation` | yes |
| `catalog_search_fallback_total` | Counter | — | yes |

### Companies

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `company_operations_total` | Counter | `operation` | yes |
| `company_errors_total` | Counter | `operation`, `reason` | yes |
| `company_latency_ms` | Histogram | `operation` | yes |

### Favorites

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `favorites_operations_total` | Counter | `operation` | yes |
| `favorites_errors_total` | Counter | `operation`, `reason` | yes |
| `favorites_latency_ms` | Histogram | `operation` | yes |

### Auth

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `auth_operations_total` | Counter | `operation` | yes |
| `auth_errors_total` | Counter | `operation`, `reason` | yes |
| `auth_latency_ms` | Histogram | `operation` | yes |

### Orders

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `order_operations_total` | Counter | `operation` | yes |
| `order_errors_total` | Counter | `operation`, `reason` | yes |
| `order_latency_ms` | Histogram | `operation` | yes |

### Products

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `product_operations_total` | Counter | `operation` | yes |
| `product_errors_total` | Counter | `operation`, `reason` | yes |
| `product_latency_ms` | Histogram | `operation` | yes |

### Reviews

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `review_operations_total` | Counter | `operation` | yes |
| `review_errors_total` | Counter | `operation`, `reason` | yes |
| `review_latency_ms` | Histogram | `operation` | yes |

### Notifications

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `notification_dispatch_total` | Counter | `template_key` | yes |
| `notification_dispatch_errors_total` | Counter | `template_key`, `reason` | yes |
| `notification_dispatch_latency_ms` | Histogram | `template_key` | yes |
| `notification_channel_deliveries_total` | Counter | `channel`, `status` | yes |
| `notification_channel_errors_total` | Counter | `channel`, `reason` | yes |

### Platform (outbox / idempotency)

| Name | Type | Labels | Alert |
|------|------|--------|-------|
| `outbox_dispatch_total` | Counter | `category` | yes |
| `outbox_dispatch_errors_total` | Counter | `category` | yes |
| `outbox_dead_letter_total` | Counter | `category` | yes |
| `idempotency_begin_total` | Counter | — | optional |
| `idempotency_conflicts_total` | Counter | `reason` | yes |
| `idempotency_replays_total` | Counter | — | yes |

## 2. OTEL ASP.NET Core (auto)

| Name (OTEL semconv) | Type | Labels | Dashboard |
|---------------------|------|--------|-----------|
| `http.server.request.duration` | Histogram | `http.method`, `http.route`, `http.status_code` | API RED |
| `http.server.active_requests` | UpDownCounter | — | API RED |

## 3. OTEL HttpClient

| Name | Type | Labels |
|------|------|--------|
| `http.client.request.duration` | Histogram | `server.address`, `http.method` |

## 4. OTEL Runtime (.NET)

| Name | Type | Dashboard |
|------|------|-----------|
| `process.runtime.dotnet.gc.collections.count` | Counter | Runtime |
| `process.runtime.dotnet.gc.heap.size` | Observable | Runtime |
| `process.runtime.dotnet.thread_pool.threads.count` | Observable | Runtime |
| `process.runtime.dotnet.exceptions.count` | Counter | Runtime |

## 5. OTEL EF Core

| Name | Type | Note |
|------|------|------|
| `db.client.operation.duration` | Histogram | No raw SQL in labels |

## 6. Infrastructure exporters (optional phase 2)

| Exporter | Metrics | Job name |
|----------|---------|----------|
| postgres_exporter | `pg_up`, `pg_stat_*` | `postgres` |
| redis_exporter | `redis_up`, `redis_memory_*` | `redis` |
| node_exporter | `node_cpu_*`, `node_memory_*` | `node` |

## 7. Collector internal

| Name | Purpose |
|------|---------|
| `otelcol_receiver_accepted_spans` | Pipeline health |
| `otelcol_exporter_send_failed_spans` | Alert on export failures |

## Cardinality policy

- **Do not** use `user_id`, `email`, `product_id` as metric labels.
- Use bounded enums: `operation`, `reason`, `job`, `channel`, `provider`.
