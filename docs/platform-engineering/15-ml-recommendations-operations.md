# 15 — ML recommendations operations

Операційний гайд для Hangfire jobs рекомендацій (`recommendation-model-train/promote/cleanup`).

## Schedule (Production defaults)

| Job | Cron (`RecommendationModel`) | Дія |
|-----|------------------------------|-----|
| Train | `0 2 * * *` | Навчання MF-моделі на interaction signals |
| Promote | `30 2 * * *` | Промоут артефакту за AUC delta |
| Cleanup | `0 4 * * *` | Видалення старих артефактів у MinIO |

Налаштування: `appsettings.Production.json` → `RecommendationModel`, `RecommendationTraining`.

## Артефакти (MinIO)

- Prefix train: `ml/recommendations/`
- Registry: `ml/recommendations/registry/`
- Promote criteria: `RecommendationTraining.MinPromotionAucDelta` (default 0.01)

## Fallback

Якщо модель відсутня або `Enabled=false`:

- Health `recommendation_model` → **Degraded**
- API використовує `FallbackMode=similar_products`

## Метрики (Prometheus / Grafana)

| Metric | Опис |
|--------|------|
| `recommendation_model_trainings_total` | Успішні train runs |
| `recommendation_model_promotions_total` | Promote events |
| `recommendation_fallbacks_total` | Fallback до similar products |
| `recommendation_inference_latency_ms` | Latency inference |

### Alert queries (приклад)

```promql
# Модель не тренувалась > 7 днів
increase(recommendation_model_trainings_total[7d]) == 0

# Багато fallback — перевірити registry / MinIO
rate(recommendation_fallbacks_total[1h]) > 10
```

## Post-deploy verification

1. `GET /health/ready` — `recommendation_model` не Critical.
2. Hangfire dashboard — останній `recommendation-model-train` Success.
3. Container test: `RecommendationPipelineContainersTests` (CI integration-full).

## Пов'язані документи

- [13-production-deploy-runbook.md](13-production-deploy-runbook.md)
- [domain-analytics-recommendations.md](../../reports/production-readiness/domain-analytics-recommendations.md)
