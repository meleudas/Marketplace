# 06 — Jaeger (distributed tracing)

## 1. Scope & non-goals

**Scope:** зберігання та UI traces з OTLP; naming conventions; log correlation.

**Non-goals:** Jaeger як metrics backend.

## 2. As-is

Tracing відсутній.

## 3. To-be

Jaeger all-in-one (dev) приймає OTLP на `4317` від Collector.

## 4. Покрокова інтеграція

1. Compose service `jaeger` — image `jaegertracing/all-in-one:1.57`.
2. Env `COLLECTOR_OTLP_ENABLED=true`.
3. UI port `16686`.
4. Collector exporter `otlp/jaeger` → `jaeger:4317`.

### Span naming

| Тип | Pattern | Приклад |
|-----|---------|---------|
| HTTP | `{method} {route template}` | `POST /api/cart/checkout` |
| EF | `db.{operation}` | `db.query` |
| HttpClient | `HTTP {method} {host}` | `HTTP POST api.sendgrid.com` |
| Hangfire | `job.{jobId}` | `job.outbox-dispatch-pending` |
| Custom | `{domain}.{operation}` | `checkout.execute` |

### Log correlation

У `Program.cs` / logging config:

```csharp
// Enricher: TraceId, SpanId з Activity.Current
```

Structured log fields: `trace_id`, `span_id` — пошук в Jaeger ↔ logs.

## 5. Конфігурація

Jaeger all-in-one defaults достатні для dev. Staging: persistent storage (Badger/Cassandra) — [10](10-staging-production-rollout.md).

## 6. Безпека

- UI не public.
- Не зберігати request body у spans.

## 7. CI/CD

Optional smoke: integration test з in-memory exporter (unit) або skip у CI.

## 8. Верифікація

1. Login → checkout flow.
2. Jaeger → Service `marketplace-api` → trace з child EF spans.

## 9. Rollback

Вимкнути trace pipeline в Collector — metrics лишаються.

## 10. Definition of Done

- [x] Traces видимі в Jaeger UI локально.
- [x] HTTP + EF spans на типовому запиті.
- [x] Custom spans: `checkout.execute`, `outbox.dispatch`, `payment.webhook.liqpay`, `cart.mutate`, Hangfire `job.*`.
