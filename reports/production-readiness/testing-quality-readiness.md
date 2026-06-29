# Testing & Quality Gates Readiness

- Статус реалізації: `Implemented`
- Готовність: **91/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

| Layer | Trait/Filter | Count | Log |
|-------|--------------|------:|-----|
| Unit | all | 443 pass | [test-unit.log](./evidence/test-unit.log) |
| Integration Light | all | 50 pass | [test-integration-light.log](./evidence/test-integration-light.log) |
| Containers | `Layer=IntegrationContainers` | 32 pass | [test-containers.log](./evidence/test-containers.log) |
| E2E | `Layer=E2E` | 35 pass | [test-e2e.log](./evidence/test-e2e.log) |

Matrix: [evidence/coverage-matrix-summary.md](./evidence/coverage-matrix-summary.md)

## Піраміда тестів

```
        E2E (35)           — HTTP smoke, seed scenarios, catalog smoke
       /        \
  Containers (32)          — Postgres/Redis/ES/MinIO/ClickHouse handlers
     /            \
Integration (50)           — SQLite handler flows
   /                \
Unit (443)                 — Domain, handlers, API, security, contracts
```

## CI quality gates

| Gate | Threshold | Ризик |
|------|-----------|-------|
| unit-coverage-gate | **10%** line (global), Phase A | Фазовий план до 25% |
| Domain gates (×15) | **12–14%** line per domain | P0 scoped 14% з `Include=` |
| integration-full | Label only | Containers/E2E не на кожному PR |
| architecture-gate | ArchUnit rules | Структурні обмеження |

## Сильні сторони

- 560 тестів, 0 failures на дату аудиту.
- 19 domain gates у CI (cart, orders, payments, shipping, coupons, reports, chats, …).
- Container suite покриває ClickHouse analytics + ML recommendations pipeline.
- ENDPOINT_COVERAGE_MATRIX з колонками U/L/C/E.

## Blockers (P0)

- Немає (тести проходять), але **низькі coverage thresholds** — процесний ризик, не технічний blocker.

## Near-term (P2)

- Підняти global unit threshold Phase B/C (15% → 25%+).
- Зробити `integration-full` обов'язковим для merge у `main` (або nightly).
- Додати migration smoke test після deploy compose.

## Optional (P2)

- Mutation testing на payment/checkout handlers.
- Contract tests для public OpenAPI vs реалізація.

## Checklist

- [x] Unit + Integration + Containers + E2E проєкти
- [x] Suite traits для CI фільтрації
- [x] Endpoint coverage matrix
- [x] Security + contract + performance suites
- [ ] Coverage thresholds production-grade
- [x] Mandatory container suite on main branch (`integration-full-main`)
- [x] Release workflow with integration-full (`backend-release.yml`)
