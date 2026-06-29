# Domain Report: Behavior Analytics & Recommendations

- Статус реалізації: `Implemented`
- Готовність: **88/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `behavior-analytics-gate`
- Container: ClickHouseWarehousePipelineContainersTests, RecommendationPipelineContainersTests, BehaviorAnalyticsIngestionContainersTests
- Infrastructure: ClickHouseAnalyticsWarehouseWriter, RecommendationModelJobs, MlNetPersonalizedRecommendationService

## Межі домену

- `backend/src/Marketplace.Domain/Behavior`
- `backend/src/Marketplace.Application/Behavior`
- `AnalyticsController`, recommendation queries
- ClickHouse warehouse, MinIO model registry

## Що готово

- Event ingest → user-item signals → funnel daily aggregates.
- ML train/validate/promote/load з MinIO `model.zip`.
- Inference з fallback коли модель відсутня.
- ClickHouse + ML у prod compose.

## Blockers (P0)

- Немає (fallback забезпечує degraded mode).

## Near-term (P1)

- Operational dashboard для signal lag і model freshness.
- Scheduled training job monitoring у prod.

## Checklist

- [x] ClickHouse pipeline container test
- [x] ML pipeline container test
- [x] Graceful fallback
- [x] Health check recommendation_model
- [x] Production model training schedule documented — [15-ml-recommendations-operations.md](../../docs/platform-engineering/15-ml-recommendations-operations.md)
