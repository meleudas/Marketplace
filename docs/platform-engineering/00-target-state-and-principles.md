# 00 — Цільовий стан і принципи

## 1. Scope & non-goals

### Scope

- Backend `Marketplace.API` та шари Domain / Application / Infrastructure.
- Метрики (RED/USE + business SLI), distributed traces, базова кореляція logs ↔ traces.
- Статичний аналіз (SonarQube CE) та архітектурні тести (NetArchTest).
- Локальний стек (Docker Compose) і опис rollout на staging/production.

### Non-goals

- Повний frontend observability (Next.js) — окремий optional етап.
- Commercial SonarQube (branch analysis, PR decoration enterprise).
- Long-term log storage (Loki/Elastic) — лише OTLP-ready; Loki як optional.
- Synthetics / RUM — поза цим планом.

## 2. As-is

| Область | Стан |
|---------|------|
| Metrics | OTEL metrics + Prometheus exporter у API; custom `Marketplace.Observability` meter |
| `/metrics` | Admin JWT; не підходить для Prometheus scrape |
| Traces | Відсутні |
| Logs OTLP | Відсутні |
| Collector / Prom / Jaeger / Grafana | Відсутні в Compose |
| Sonar / NetArchTest | Відсутні |
| CI | Coverlet cobertura per domain; без Sonar scanner |

## 3. To-be

### Принципи

1. **OTLP-first** — єдиний egress з додатку: gRPC/HTTP OTLP на Collector.
2. **Separation of concerns** — API не знає про Prometheus/Jaeger напряму.
3. **Defense in depth** — NetArchTest (структура) + Sonar (bugs/smells) + domain test gates (поведінка).
4. **Low cardinality** — не писати `user_id`, email, JWT у labels/spans.
5. **PII-safe telemetry** — scrubbing на Collector + фільтри в SDK.

### RED (HTTP)

- **Rate** — `http.server.request.duration` count / RPS.
- **Errors** — 5xx ratio, `*_errors_total` business counters.
- **Duration** — p50/p95/p99 histograms.

### USE (інфра)

- **Utilization** — CPU/memory (runtime metrics), DB connections (EF).
- **Saturation** — thread pool queue, Redis timeouts.
- **Errors** — exception counters, dependency failures.

### SLI / SLO (орієнтири для staging)

| SLI | Target (staging) | Вимір |
|-----|------------------|-------|
| API availability | 99.5% / 30d | `up{job="otel-collector"}` + health |
| Checkout success | 98% / 7d | `checkout_operations` − errors |
| Payment webhook processing | 99% / 7d | `webhook_operations` − errors |
| Outbox DLQ rate | < 0.1% dispatches | `outbox_dead_letter_total` |

## 4. Покрокова інтеграція (огляд)

Див. окремі файли 01–10; порядок: Collector → SDK OTLP → Prometheus/Jaeger → Grafana → NetArchTest → Sonar.

## 5. Конфігурація (глобальні env)

| Змінна | Dev (Compose) | Staging |
|--------|---------------|---------|
| `OTEL_SERVICE_NAME` | `marketplace-api` | `marketplace-api` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | internal collector URL |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | `grpc` |
| `OTEL_RESOURCE_ATTRIBUTES` | `deployment.environment=development` | `deployment.environment=staging` |

Секція `OpenTelemetry` в `appsettings.json` — див. [03-opentelemetry-dotnet-sdk.md](03-opentelemetry-dotnet-sdk.md).

## 6. Безпека

- Collector і Prometheus — лише internal network / VPN.
- Не експортувати Authorization, Cookie, API keys у span attributes.
- `/metrics` legacy — вимкнено за замовчуванням (`Observability:EnableLegacyPrometheusEndpoint: false`).
- Sonar token — GitHub secret, не в репо.

## 7. CI/CD

Усі quality/observability jobs описані в [08-ci-cd-integration.md](08-ci-cd-integration.md). Domain gates (`Suite=CartCheckout`, …) залишаються незалежними.

## 8. Верифікація

- `curl http://localhost:9090/-/healthy` — Prometheus up.
- Jaeger UI `http://localhost:16686` — trace після `POST /api/cart/checkout`.
- Grafana `http://localhost:3001` — datasource Prometheus + panels з `cart_operations_total`.

## 9. Rollback / troubleshooting

- Вимкнути OTLP: `OpenTelemetry:Enabled=false` — API працює без telemetry.
- Collector down — SDK buffer + drop (не блокувати requests); перевірити `otelcol` logs.
- High cardinality — перевірити custom tags у controllers.

## 10. Definition of Done

- [x] Документація 00–10 + appendices опублікована.
- [x] OTLP metrics+traces+logs до Collector у dev Compose (`docker-compose.observability.yml`).
- [x] Grafana показує business + RED panels (5 dashboards, 11 alert rules).
- [x] `architecture-gate`, `observability-config-validate` і `sonar-analysis` у CI (Sonar — `if: vars.SONAR_HOST_URL` + secret).
