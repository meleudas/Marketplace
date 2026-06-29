# 14 — Staging environment

Staging — окреме середовище для sandbox-інтеграцій (LiqPay, Nova Poshta) без prod-секретів.

## Конфігурація

| Джерело | Призначення |
|---------|-------------|
| `backend/src/Marketplace.API/appsettings.Staging.json` | Базові прапорці (OTel, moderation, rate limits) |
| `ASPNETCORE_ENVIRONMENT=Staging` | Профіль запуску API |
| GitHub Secrets (staging E2E workflow) | `LIQPAY_SANDBOX_PUBLIC`, `LIQPAY_SANDBOX_PRIVATE`, `NOVAPOSHTA_API_KEY` |

## Локальний запуск (compose)

```bash
export ASPNETCORE_ENVIRONMENT=Staging
export LIQPAY_SANDBOX_PUBLIC=...
export LIQPAY_SANDBOX_PRIVATE=...
export NOVAPOSHTA_API_KEY=...
docker compose -f docker-compose.dev.yml up -d
dotnet run --project backend/src/Marketplace.API
```

## Staging E2E тести

```powershell
$env:LIQPAY_SANDBOX_PUBLIC = "..."
$env:LIQPAY_SANDBOX_PRIVATE = "..."
$env:NOVAPOSHTA_API_KEY = "..."
dotnet test backend/tests/Marketplace.Tests.E2E --filter "Suite=Staging&Layer=E2E"
```

Тести **no-op pass**, якщо secrets не задані (локальна розробка без sandbox keys).

## CI

Workflow [backend-staging-e2e.yml](../../.github/workflows/backend-staging-e2e.yml):

- `workflow_dispatch`, nightly `cron`, push до `staging`
- `continue-on-error: true` (informational до стабілізації)
- Filter: `Suite=Staging&Layer=E2E`

## Ротація sandbox keys

1. Оновити keys у LiqPay / Nova Poshta кабінетах (test mode).
2. Оновити GitHub Secrets у repo settings.
3. Перезапустити `backend-staging-e2e` workflow.
4. Перевірити green run 7+ днів перед public launch.

## Пов'язані документи

- [13-production-deploy-runbook.md](13-production-deploy-runbook.md)
- [12-production-secrets-policy.md](12-production-secrets-policy.md)
