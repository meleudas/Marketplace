# 05 — Prometheus

## 1. Scope & non-goals

**Scope:** TSDB для метрик з Collector; recording rules; scrape config.

**Non-goals:** Scrape `api:8080/metrics` (захищений JWT).

## 2. As-is

Prometheus не розгорнутий.

## 3. To-be

Prometheus scrapes **only** `otel-collector:8889` (+ optional infra exporters).

## 4. Покрокова інтеграція

1. Файл [`observability/prometheus.yml`](../../observability/prometheus.yml).
2. Compose service `prometheus` profile `observability`, port `9090`.
3. Volume для TSDB retention 15d (dev).
4. Optional: `postgres_exporter`, `redis_exporter` — phase 2 у metrics-catalog.

## 5. Конфігурація

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: otel-collector
    static_configs:
      - targets: ['otel-collector:8889']

  - job_name: prometheus
    static_configs:
      - targets: ['localhost:9090']
```

Recording rules: [`observability/prometheus-rules.yml`](../../observability/prometheus-rules.yml)

Приклади:

```yaml
groups:
  - name: marketplace_recording
    interval: 1m
    rules:
      - record: marketplace:http_server_duration_p95:5m
        expr: histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le, http_route))
      - record: marketplace:checkout_error_rate:10m
        expr: sum(rate(checkout_errors_total[10m])) / sum(rate(checkout_operations_total[10m]))
```

## 6. Безпека

- UI `9090` — localhost / VPN only.
- No remote write з секретами в репо.

## 7. CI/CD

`promtool check rules observability/prometheus-rules.yml` у `observability-config-validate`.

## 8. Верифікація

- `http://localhost:9090/targets` — `otel-collector` UP.
- Query: `checkout_operations_total`.

## 9. Rollback

Видалити profile `observability` — API без Prometheus не ламається.

## 10. Definition of Done

- [x] prometheus.yml + rules у репо.
- [x] Targets healthy у dev Compose.
- [x] PromQL з ObservabilityRunbook перевірені на реальних series names.
