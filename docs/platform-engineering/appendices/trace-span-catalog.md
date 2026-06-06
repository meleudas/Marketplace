# Appendix — Каталог spans (traces)

ActivitySource: `Marketplace.Telemetry` (custom).  
Auto instrumentation: ASP.NET Core, HttpClient, EF Core.

## HTTP (auto)

| Span name | Parent | Tags |
|-----------|--------|------|
| `{method} {route}` | — | `http.method`, `http.route`, `http.status_code` |

**Filtered routes (no trace):** `/health`, `/metrics`, `/swagger`, `/openapi`, `/hangfire` (configurable).

## Database (EF auto)

| Span name | Tags |
|-----------|------|
| `db.query` / `db.client.operation` | `db.system=postgresql`, `db.name=marketplace` |

`db.statement` — disabled in production.

## Outbound HTTP (auto)

| Span name | Example |
|-----------|---------|
| `HTTP POST` | SendGrid, LiqPay, Elasticsearch, MinIO |

## Custom — Cart / Checkout

| Span | Operation tag | When |
|------|---------------|------|
| `checkout.execute` | `checkout` | `CheckoutCartCommandHandler` |
| `cart.mutate` | `add_item`, `update_item_quantity`, … | Cart handlers |

## Custom — Payments

| Span | When |
|------|------|
| `payment.webhook.liqpay` | LiqPay webhook handler |
| `payment.sync.pending` | Hangfire `payments-sync-pending-liqpay` |

## Custom — Platform

| Span | When |
|------|------|
| `outbox.dispatch` | `OutboxDispatcherJobs` |
| `idempotency.begin` | HTTP idempotency middleware (short span) |
| `hangfire.job` | Wrapper для recurring jobs |

## Custom — Notifications

| Span | Tags |
|------|------|
| `notification.dispatch` | `template_key`, `channel` (no user email) |

## Propagation

- **W3C** `traceparent` / `tracestate` на вхідних HTTP.
- **Baggage allowlist:** `correlation_id`, `company_id` (UUID only).

## Log correlation fields

| Field | Source |
|-------|--------|
| `trace_id` | `Activity.Current.TraceId` |
| `span_id` | `Activity.Current.SpanId` |
| `correlation_id` | Business idempotency / notification correlation |

## Verification checklist

- [x] Login → HTTP + EF child spans (ASP.NET instrumentation).
- [x] Checkout → trace з EF + custom `checkout.execute`.
- [x] Webhook replay → `payment.webhook.liqpay` linked to parent HTTP span.
