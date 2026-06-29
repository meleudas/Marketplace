# P1 completion log

- **Дата:** 2026-06-29
- **План:** `p1_reports_execution_02bcc110`

## Workstreams

| ID | Задача | Статус | Evidence |
|----|--------|--------|----------|
| P1-1 | Coverage gates Phase A (global 10, P0 scoped 14) | Done | [coverage-baseline-2026-06-29.md](./coverage-baseline-2026-06-29.md), `backend-ci.yml` |
| P1-2 | Staging E2E LiqPay + Nova Poshta | Done | `LiqPaySandboxE2ETests`, `NovaPoshtaSandboxE2ETests`, [backend-staging-e2e.yml](../../.github/workflows/backend-staging-e2e.yml), [14-staging-environment.md](../../docs/platform-engineering/14-staging-environment.md) |
| P1-3 | Anti-abuse (reviews, payments webhook, notifications) | Done | `CreateRateLimitPolicy`, domain policies, `abuse_rejected_total`, `AntiAbusePolicyTests` |
| P1-4 | Postgres backup/restore | Done | `backend/scripts/ops/postgres-backup.sh`, `docker-compose.ops.yml`, runbook § backup |
| P1-5 | Returns refund container + E2E | Done | `ReturnRefundLedgerPostgresTests`, `ReturnsRefundWorkflowE2ETests` |
| P1-6 | ML recommendations ops | Done | [15-ml-recommendations-operations.md](../../docs/platform-engineering/15-ml-recommendations-operations.md) |
| P1-7 | Branch protection docs | Done | [16-github-branch-protection.md](../../docs/platform-engineering/16-github-branch-protection.md) |
| P1-8 | Reports update | Done | executive + domain reports |

## Branch protection (manual)

- [ ] Repo admin підтвердив required check `integration-full-main` на `main` ([16-github-branch-protection.md](../../docs/platform-engineering/16-github-branch-protection.md))

## Staging E2E (manual)

- [ ] GitHub Secrets `LIQPAY_SANDBOX_*`, `NOVAPOSHTA_API_KEY` налаштовані
- [ ] Nightly `backend-staging-e2e` green 7+ днів

## Верифікація

```powershell
dotnet test backend/tests/Marketplace.Tests.Unit -c Release --filter "Suite=Security|Suite=Reviews"
dotnet test backend/tests/Marketplace.Tests.Integration.Containers -c Release --filter "Suite=Returns"
dotnet test backend/tests/Marketplace.Tests.E2E -c Release --filter "Suite=Returns&Layer=E2E"
```
