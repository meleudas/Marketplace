# 09 — Локальний Docker Compose runbook

## 1. Scope & non-goals

**Scope:** profiles `observability` та `sonar` для dev на Windows/macOS/Linux.

## 2. As-is

[`docker-compose.dev.yml`](../../docker-compose.dev.yml) — api, postgres, redis, elasticsearch, minio.

Моніторинг і SonarQube — [`docker-compose.monitoring.yml`](../../docker-compose.monitoring.yml) (profiles `observability`, `sonar`).

## 3. To-be

Профілі в окремому compose; API з OTLP env через [`docker-compose.observability.yml`](../../docker-compose.observability.yml).

## 4. Покрокова інтеграція

### Повний dev stack

```powershell
# з кореня репозиторію (API з OTEL_ENABLED через override)
docker compose -f docker-compose.dev.yml -f docker-compose.monitoring.yml -f docker-compose.observability.yml --profile observability up -d --build
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
