# Gap Domains and Proposals

Оновлено: **2026-06-29** (P1 закрито — [evidence/p1-completion-log.md](./evidence/p1-completion-log.md)).

## Зведення залишкових ризиків

| Область | Поточний стан | Орієнтовна готовність | Пріоритет |
|---|---|---:|---|
| Support (повний helpdesk) | Partial — port contract + domain | 72/100 | P2 |
| Coverage gates Phase B/C | Global 10%, target 25% | 75/100 | P2 |
| Full CD (registry/K8s) | Release workflow + manual runbook | 80/100 | P2 |
| Staging E2E ops | Workflow + tests; потрібні secrets + 7d green | 85/100 | Ops |
| Branch protection | Documented; потрібне admin confirm | 85/100 | Ops |

## P1 — CLOSED (code, 2026-06-29)

1. ~~Coverage gates Phase A~~ → global 10%, P0 scoped 14%
2. ~~Staging E2E~~ → `backend-staging-e2e.yml`, Suite=Staging tests
3. ~~Anti-abuse~~ → reviews, payments webhook, notifications
4. ~~Postgres backup~~ → scripts + `docker-compose.ops.yml`
5. ~~Returns refund E2E~~ → container + E2E
6. ~~ML ops doc~~ → docs/15
7. ~~Branch protection doc~~ → docs/16

## Рекомендований порядок доробок (P2+)

1. Coverage Phase B/C (25%+).
2. Full CD to registry/K8s.
3. Support public API.
4. DAST / penetration test.

## Домени без окремого container-тесту

- Chats — є integration + E2E seed, немає `Layer=IntegrationContainers` класу.
- Support — лише `SupportHelpdeskPortContractTests`.
- Admin coupon CRUD — unit/light, без container flow.
