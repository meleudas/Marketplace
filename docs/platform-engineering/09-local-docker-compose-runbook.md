# 09 — Локальний Docker Compose runbook

## 1. Scope & non-goals

**Scope:** profiles `observability` та `sonar` для dev на Windows/macOS/Linux.

## 2. As-is

[`docker-compose.dev.yml`](../../docker-compose.dev.yml) — api, postgres, redis, minio.

Elasticsearch (product search) — [`docker-compose.elasticsearch.yml`](../../docker-compose.elasticsearch.yml) (опційний overlay).

ClickHouse (analytics) — [`docker-compose.clickhouse.yml`](../../docker-compose.clickhouse.yml) (опційний overlay).

Моніторинг і SonarQube — [`docker-compose.monitoring.yml`](../../docker-compose.monitoring.yml) (profiles `observability`, `sonar`).

## 3. To-be

Профілі в окремому compose; API з OTLP env через [`docker-compose.observability.yml`](../../docker-compose.observability.yml).

## 4. Покрокова інтеграція

### Elasticsearch (product search)

```powershell
# тільки Elasticsearch
docker compose -f docker-compose.elasticsearch.yml up -d

# разом із dev stack
$env:ELASTICSEARCH__ENABLED = "true"
docker compose -f docker-compose.dev.yml -f docker-compose.elasticsearch.yml up -d --build
# HTTP http://localhost:9200
```

Без цього overlay API працює з `Elasticsearch__Enabled=false` і використовує DB fallback для каталогу. Для зовнішнього кластера (або AWS OpenSearch) задай `Elasticsearch__Url` / `Elasticsearch__Enabled` у `backend/.env`.

Або додай у root `.env`: `ELASTICSEARCH__ENABLED=true` перед `docker compose ... -f docker-compose.elasticsearch.yml`.

### ClickHouse (analytics)

```powershell
# тільки ClickHouse
docker compose -f docker-compose.clickhouse.yml up -d

# разом із dev stack
docker compose -f docker-compose.dev.yml -f docker-compose.clickhouse.yml up -d --build
# HTTP http://localhost:8123 , native localhost:9009
```

Для API в Docker увімкни ClickHouse через `backend/.env` (див. коментарі в `docker-compose.clickhouse.yml`). Для `dotnet run` на хості достатньо `appsettings.Development.json` (`http://localhost:8123`).

### Повний dev stack

```powershell
# з кореня репозиторію (API з OTEL_ENABLED через override)
$env:ELASTICSEARCH__ENABLED = "true"
docker compose -f docker-compose.dev.yml -f docker-compose.elasticsearch.yml -f docker-compose.monitoring.yml -f docker-compose.observability.yml --profile observability up -d --build
```

### Тільки observability (API вже запущений)

```powershell
docker compose -f docker-compose.dev.yml -f docker-compose.monitoring.yml --profile observability up -d otel-collector prometheus jaeger grafana
```

### SonarQube

```powershell
docker compose -f docker-compose.dev.yml -f docker-compose.monitoring.yml --profile sonar up -d
# http://localhost:9002
```

### Порти

| Сервіс | URL |
|--------|-----|
| API | http://localhost:8080 |
| Elasticsearch | http://localhost:9200 (окремий compose) |
| Grafana | http://localhost:3001 |
| Prometheus | http://localhost:9090 |
| Jaeger UI | http://localhost:16686 |
| SonarQube | http://localhost:9002 |
| Collector OTLP gRPC | localhost:4317 |

### Env для API (автоматично в compose)

```yaml
OTEL_SERVICE_NAME: marketplace-api
OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL: grpc
OpenTelemetry__Enabled: "true"
OpenTelemetry__EnableLegacyPrometheusEndpoint: "false"
```

### Smoke test

```powershell
# Автоматична перевірка health + Prometheus targets + Jaeger
backend/scripts/observability-smoke.ps1
```

```powershell
# Health
curl http://localhost:8080/health

# Prometheus targets
start http://localhost:9090/targets

# Jaeger — зробити login, перевірити trace
start http://localhost:16686

# Grafana (admin / пароль з .env.example)
start http://localhost:3001
```

## 5. Конфігурація

Див. `observability/`, [`docker-compose.monitoring.yml`](../../docker-compose.monitoring.yml) та [`docker-compose.observability.yml`](../../docker-compose.observability.yml).

## 6. Безпека

Не комітити `GRAFANA_ADMIN_PASSWORD` — лише `.env` локально.

## 7. CI/CD

N/A (локальний runbook).

## 8. Верифікація

- [x] Усі контейнери healthy.
- [x] Prometheus target `otel-collector` UP.
- [x] Business metric visible після API traffic.

## 9. Rollback

```powershell
docker compose -f docker-compose.dev.yml -f docker-compose.monitoring.yml --profile observability down
```

## 10. Definition of Done

- [x] Runbook перевірений на чистій машині (див. `observability-smoke.ps1`).
- [x] Посилання в [`backend/DOCKER_COMMANDS.md`](../../backend/DOCKER_COMMANDS.md).
