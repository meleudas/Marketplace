# Observability Runbook

## Metrics endpoint

- `GET /metrics` exposes Prometheus-compatible OpenTelemetry metrics.
- Includes ASP.NET Core, HttpClient, runtime and custom marketplace meters.

## Key SLI

- `cache_hits_total`, `cache_misses_total`, `cache_errors_total`, `cache_latency_ms`
- `payment_operations_total`, `payment_errors_total`, `payment_latency_ms`
- `webhook_operations_total`, `webhook_errors_total`, `webhook_latency_ms`
- `hangfire_jobs_total`, `hangfire_job_errors_total`, `hangfire_job_latency_ms`

## Suggested alerts

- cache miss ratio spikes (`miss / (hit + miss)`) over baseline.
- webhook or payment p95 latency increase (`*_latency_ms` histogram).
- sustained growth in `*_errors_total` for payment/webhook/hangfire.
