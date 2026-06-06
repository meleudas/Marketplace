# 07 — Grafana (dashboards + alerts)

## 1. Scope & non-goals

**Scope:** provisioning datasources, dashboards, unified alerting з ObservabilityRunbook.

**Non-goals:** On-call paging integration (описати webhook placeholder).

## 2. As-is

Grafana відсутня; алерти лише текстом у ObservabilityRunbook.

## 3. To-be

Grafana `3001` з auto-provisioned Prometheus + Jaeger datasources.

## 4. Покрокова інтеграція

Структура:

```
observability/grafana/provisioning/
  datasources/datasources.yaml
  dashboards/dashboards.yaml
  dashboards/json/
    api-red.json
    runtime-dotnet.json
    business-sli.json
    platform-dlq.json
    hangfire-jobs.json
```

### Dashboards

| ID | Назва | Panels |
|----|-------|--------|
| D1 | API RED | RPS, p95 latency, 5xx ratio |
| D2 | .NET Runtime | GC, heap, thread pool |
| D3 | Business SLI | cart, checkout, payments, orders, … |
| D4 | Platform DLQ | outbox_dead_letter, idempotency_conflicts |
| D5 | Hangfire Jobs | per `job` label errors/latency |

### Alerts (приклади → PromQL)

| Runbook bullet | PromQL (скорочено) | `for` |
|----------------|-------------------|-------|
| Checkout error > 2% | `rate(checkout_errors_total[10m])/rate(checkout_operations_total[10m]) > 0.02` | 10m |
| Outbox DLQ growth | `increase(outbox_dead_letter_total[1h]) > 10` | 10m |
| Payment webhook errors | `rate(webhook_errors_total{provider="liqpay"}[10m]) > 0.1` | 10m |
| Cart p95 latency | `histogram_quantile(0.95, rate(cart_latency_ms_bucket[5m])) > 500` | 10m |

Повний mapping — [appendices/metrics-catalog.md](appendices/metrics-catalog.md).

## 5. Конфігурація

`datasources.yaml`:

```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    url: http://prometheus:9090
    isDefault: true
  - name: Jaeger
    type: jaeger
    url: http://jaeger:16686
```

Admin: `GF_SECURITY_ADMIN_USER=admin`, password з env `GRAFANA_ADMIN_PASSWORD` (не в git).

## 6. Безпека

- Anonymous access disabled.
- Dashboards read-only для viewers.

## 7. CI/CD

JSON dashboards — review у PR; optional `grafana-tool validate` later.

## 8. Верифікація

- Grafana → Explore → Prometheus → `cart_operations_total`.
- Alert test: знизити threshold тимчасово → firing state.

## 9. Rollback

Видалити grafana service з compose.

## 10. Definition of Done

- [x] 5 dashboards provisioned.
- [x] 11 alert rules з ObservabilityRunbook (`observability/grafana/provisioning/alerting/rules.yaml`).
- [x] Jaeger datasource для trace drill-down.
