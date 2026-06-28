# Appendix — SonarQube Quality Gate (CE)

Проєкт: `marketplace-backend`.

## Quality Gate conditions (рекомендовані для CE)

| Metric | Operator | Threshold | Scope |
|--------|----------|-----------|-------|
| New bugs | = | 0 | New Code |
| New vulnerabilities | = | 0 | New Code |
| New blocker/critical issues | = | 0 | Overall (new) |
| Coverage on New Code | ≥ | 60% | New Code |
| Duplicated Lines on New Code | ≤ | 3% | New Code |
| Maintainability Rating on New Code | = | A | New Code |
| Reliability Rating on New Code | = | A | New Code |
| Security Rating on New Code | = | A | New Code |

> Поріг coverage 60% на **New Code** не замінює Coverlet domain gates (8–12% line на suite) — Sonar фокус на зміни в PR.

## Exclusions (analysis)

```
**/Migrations/**
**/bin/**
**/obj/**
**/Contracts/**
**/test-results/**
```

## Exclusions (coverage)

Tests assembly не в `sonar.sources`; coverage paths лише з test output.

## C# rules highlights

- S6964 — async void avoid (handlers).
- S107 — too many parameters (domain factories).
- Security hotspots — SQL injection via EF parameterized (review raw SQL if any).

## CE limitations

- **Branch analysis** обмежений — використовувати:
  - `sonar.branch.name=${{ github.head_ref }}` у scanner begin (якщо підтримується плагіном)
  - або **New Code Period** = Previous version
- **PR decoration** — потребує AlmIntegration або manual comment з quality gate API.

## Quality Gate API check (CI)

```bash
curl -u "$SONAR_TOKEN:" \
  "$SONAR_HOST_URL/api/qualitygates/project_status?projectKey=marketplace-backend"
```

Fail job if `projectStatus.status != OK`.

## Bootstrap Sonar (first run)

1. Admin password change.
2. Install plugin **C#** (bundled in recent CE).
3. Create project `marketplace-backend` manual or via scanner.
4. Set New Code Definition: *Previous version* or *Number of days* = 30.
