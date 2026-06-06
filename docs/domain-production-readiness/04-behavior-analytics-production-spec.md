# 04 - Behavior and Analytics Production Spec

## 1. Context and business goals

Behavior/Analytics потрібен для data-driven рішень у каталозі, пошуку, retention і anti-fraud.

Ціль - стабільний збір подій (`ProductView`, `SearchHistory`) та агрегати для BI/операційних dashboard-ів.

## 2. Domain target state

Наявні сутності:

- `Marketplace.Domain/Behavior/Entities/ProductView.cs`
- `Marketplace.Domain/Behavior/Entities/SearchHistory.cs`
- `Marketplace.Domain/Analytics/Entities/AnalyticsEvent.cs`
- `Marketplace.Domain/Analytics/Entities/ActivityLog.cs`

Цільовий стан:

- unified `BehaviorEvent` контракт для view/search/click/add-to-cart.
- `UserBehaviorDaily` агрегат для денних KPI.
- taxonomy подій із versioning (`event_version`) для backward compatibility.

## 3. Application target state

### Команди/запити

- `TrackProductView`
- `TrackSearchQuery`
- `TrackCatalogInteraction`
- `GetBehaviorSummary`
- `GetTopQueries`
- `GetConversionFunnel`

### Політики

- write path асинхронний (queue/outbox) для low-latency API.
- дедуп подій у короткому часовому вікні.
- sampling policy для high-volume подій.

## 4. API target state

### Endpoints

- `POST /analytics/events`
- `POST /analytics/search-history`
- `GET /admin/analytics/kpi/summary`
- `GET /admin/analytics/kpi/funnel`

### Authz

- public/user events - anonymous or authenticated with strict payload validation.
- admin endpoints - `Admin` only.

### Error model

- `400` malformed event schema.
- `413` oversized payload.
- `429` event ingestion rate exceeded.

## 5. Infrastructure and data model

### Таблиці/сховища

- `behavior_events_raw` (append-only)
- `behavior_events_dedup`
- `behavior_daily_aggregates`
- `search_query_aggregates`

### Pipeline

- ingestion -> validation -> enrichment -> aggregation.
- delayed jobs для batch aggregation.
- retention:
  - raw events: 30-90 днів;
  - aggregates: 12+ місяців.

## 6. Security and privacy

- PII redaction:
  - не писати email/phone/JWT у payload.
- consent-aware tracking policy.
- data subject deletion path для user-id based events.

## 7. Testing strategy and CI gates

### Unit (Suite=BehaviorAnalytics)

- schema validation;
- dedup/sampling rules;
- KPI aggregation correctness.

### IntegrationLight

- ingest -> aggregate -> read KPI summary.

### IntegrationContainers

- high-volume ingestion stability;
- queue lag/retry behavior.

### E2E

- catalog view/search дії відображаються в admin KPI endpoint.

### CI gate

- `behavior-analytics-gate`:
  - Unit + IntegrationLight (`Suite=BehaviorAnalytics`)
  - Coverage threshold >= 12%.

## 8. Observability and runbook

### Метрики

- `analytics_events_ingested_total`
- `analytics_events_dropped_total`
- `analytics_pipeline_latency_ms`
- `analytics_aggregation_failures_total`

### Алерти

- drop rate > threshold;
- aggregation lag > threshold.

### Runbook

- backfill procedure for missing windows;
- replay from raw events.

## 9. Release and rollback strategy

- flags:
  - `BehaviorTrackingEnabled`
  - `AdminAnalyticsReadEnabled`
- rollout:
  1. ingestion only;
  2. aggregates;
  3. admin KPI APIs.
- rollback: disable ingestion endpoint and switch to safe sampling profile.

## 10. Definition of Done (100/100)

- [ ] Є стабільний ingestion + aggregation pipeline.
- [ ] KPI endpoint-и мають валідацію й authz.
- [ ] PII політика виконується технічно (не декларативно).
- [ ] Є Unit/Integration/E2E покриття і `Suite=BehaviorAnalytics`.
- [ ] Є алерти на drop rate і lag.
