# Security & Compliance Readiness

- Статус реалізації: `Implemented`
- Готовність: **91/100**
- Release status: `Ready`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI job `security-regression`: `Suite=Security` ([backend-ci.yml](../../.github/workflows/backend-ci.yml))
- [evidence/test-unit.log](./evidence/test-unit.log)
- Rate limiting: RedisRateLimitingContainersTests
- Auth container: AuthFlowPostgresTests, ExternalAuthCallbackPostgresTests
- Vuln scan: [evidence/vulnerable-packages.txt](./evidence/vulnerable-packages.txt)

## Контроли

| Область | Реалізація | Оцінка | Примітки |
|---------|------------|--------|----------|
| Authentication | JWT + refresh rotation, ASP.NET Identity | 88 | `RequireConfirmedEmail` default `true` у DI |
| Authorization | Domain access services (products, orders, companies) | 86 | Покрито unit + integration |
| Webhook security | LiqPay HMAC, Telegram secret header | 85 | E2E telegram webhook |
| Anti-abuse | CreateRateLimitPolicy + domain policies | 88 | reviews, payments webhook, notifications; `abuse_rejected_total` |
| HTTP idempotency | HttpIdempotencyStore | 90 | Platform gate + tests |
| CORS | Configurable origins | 80 | Dev defaults у .env.example |
| Admin surface | Hangfire, metrics, outbox admin | 84 | Потрібна authz перевірка в prod |
| Secrets | .env.example, no committed secrets | 72 | JWT dev default у example |
| Dependencies | NU1902/NU1903 transitive | 78 | OpenTelemetry moderate; SQLite high (tests only) |

## Що готово

- Окремий `security-regression` job у CI на кожен PR.
- Soft-delete блокує login; reporter не може модерувати власний report.
- Refresh token revocation на logout (AuthFlowPostgresTests).
- `check_vulnerabilities.py` блокує High/Critical у CI (коли Python доступний).

## Blockers (P0)

- ~~Замінити dev JWT secret у production~~ **CLOSED** — `ProductionConfigurationValidator` fail-fast + [12-production-secrets-policy.md](../../docs/platform-engineering/12-production-secrets-policy.md)

## Near-term (P2)

- Оновити OpenTelemetry пакети після виходу патчів (4× moderate GHSA).

## Optional (P2)

- OWASP ZAP / DAST у staging pipeline.
- Security headers audit (CSP, HSTS) на API gateway.

## Checklist

- [x] Password hashing (Identity)
- [x] Refresh token lifecycle
- [x] Webhook signature validation
- [x] Rate limiting implementation
- [x] Security test suite у CI
- [x] Prod secrets policy formalized
- [ ] Dependency patch cadence для OpenTelemetry
