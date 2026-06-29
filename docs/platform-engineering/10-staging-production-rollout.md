# 10 — Staging / Production rollout

## 1. Scope & non-goals

**Scope:** розгортання observability + Sonar на VM/K8s; TLS; retention; backup.

**Non-goals:** Детальний Helm chart (лише принципи).

## 2. As-is

- Dev Compose: [09-local-docker-compose-runbook.md](09-local-docker-compose-runbook.md)
- Production deploy: [13-production-deploy-runbook.md](13-production-deploy-runbook.md)
- Secrets policy: [12-production-secrets-policy.md](12-production-secrets-policy.md)
- Release CI: [backend-release.yml](../../.github/workflows/backend-release.yml)

## 3. To-be

| Компонент | Staging | Production |
|-----------|---------|------------|
| Collector | 1 replica, internal LB | 2+ replicas, HPA |
| Prometheus | 30d retention, 50GB | 90d, remote write optional |
| Jaeger | Badger persistent | Cassandra/Elasticsearch backend |
| Grafana | SSO optional | SSO required |
| SonarQube | Dedicated VM | Dedicated VM + weekly backup |
| OTLP | mTLS | mTLS + network policies |

## 4. Покрокова інтеграція

### 4.1 Collector sidecar (K8s pattern)

- Container `otel-collector` поруч з `marketplace-api`.
- `OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:4317`.

### 4.2 Secrets (GitHub / vault)

- `SONAR_TOKEN`
- `GRAFANA_ADMIN_PASSWORD`
- `OTEL_EXPORTER_OTLP_HEADERS` (якщо auth на collector)

### 4.3 SLO monitoring

Використати recording rules з [05-prometheus.md](05-prometheus.md); error budget alerts у Grafana.

### 4.4 Sonar backup

```bash
pg_dump sonarqube > sonar_backup.sql
# + volume sonarqube_data
```

### 4.5 Cost controls

- Trace sampling 10% prod.
- Metric cardinality review quarterly.

## 5. Конфігурація

У репо: [`appsettings.Staging.json`](../../backend/src/Marketplace.API/appsettings.Staging.json) (`TraceSamplingRatio: 0.5`), [`appsettings.Production.json`](../../backend/src/Marketplace.API/appsettings.Production.json) (`0.1`). Приклад override для deploy:

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "OtlpEndpoint": "https://otel-collector.internal:4317",
    "TraceSamplingRatio": 0.1,
    "EnableLegacyPrometheusEndpoint": false
  }
}
```

## 6. Безпека

- WAF не блокує internal OTLP.
- RBAC Grafana: Editor vs Viewer.
- Sonar: не аналізувати secrets у `appsettings.Production.json` — exclusion.

## 7. CI/CD

Production deploy не запускає Sonar — лише `main` + release branches.

## 8. Верифікація

- Synthetic probe: health + sample trace кожні 5m.
- On-call runbook: ObservabilityRunbook + Grafana links.

## 9. Rollback

Feature flag `OpenTelemetry:Enabled=false` через config reload / redeploy.

## 10. Definition of Done

- [x] `appsettings.Staging.json` / `appsettings.Production.json` у репо.
- [ ] Staging checklist пройдений (операційний rollout).
- [ ] Prod SLO dashboards + paging webhook (налаштування contact point у Grafana).
- [ ] Sonar backup automated.
