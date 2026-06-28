# Domain Report: Identity & Access

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Auth`
- `backend/src/Marketplace.Domain/Users`
- `backend/src/Marketplace.Application/Auth`
- `backend/src/Marketplace.API/Controllers/AuthController.cs`
- `backend/src/Marketplace.Infrastructure/Identity`

## Що вже готово

- JWT + refresh flow, external auth, email/telegram 2FA.
- Посилена Identity policy: strong password + lockout + `RequireConfirmedEmail` у production.
- Refresh replay detection + revoke активних сесій після password reset.
- Debug/health perimeter закритий: `/metrics` тільки для `Admin`, `/hangfire` і key-trace тільки `Development`.
- Повний тестовий контур `Suite=IdentityAccess`: unit/application, API, contract, SQLite integration/e2e, security, performance.
- Додано auth observability (`auth_operations_total`, `auth_errors_total`, `auth_latency_ms`) і оновлено runbook.
- Окремий CI quality gate: `identity-access-gate` (coverage threshold 12%).

## Blockers

- Blockers закрито.

## Near-term

- Підтримувати доменний gate `Suite=IdentityAccess` без зниження coverage.
- Додати алерти на replay (`reason=replay_detected`) у production моніторинг.

## Optional

- Розширити audit trail на auth-події (user-agent/IP fingerprint для підозрілих refresh сценаріїв).

## Мінімальний checklist

- [x] Password policy відповідає прод-вимогам.
- [x] `RequireConfirmedEmail=true` у production.
- [x] Є API тести на 401/403 та refresh rotation.
- [x] Секрети JWT/2FA не в репозиторії.
