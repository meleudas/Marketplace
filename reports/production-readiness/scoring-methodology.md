# Методика оцінки продакшен-готовності

Аудит: **2026-06-29** (evidence-based переоцінка).

## Шкала

- Кожен домен або наскрізний стовп оцінюється у діапазоні `0..100`.
- `90-100`: production-ready з мінімальними ризиками.
- `75-89`: можна релізити з контрольованим ризиком (**Conditional Go**).
- `60-74`: рання production-ready фаза, потрібні обов'язкові доробки.
- `40-59`: частково готовий, реліз ризикований.
- `0-39`: концепт або незавершена реалізація.

## Рівень 1 — критерії всередині домену / стовпа

| Критерій | Вага |
|---|---:|
| Architecture & Code Health | 20% |
| Security & Access Control | 20% |
| Data & Migrations | 15% |
| API Robustness & Error Handling | 15% |
| Testing & CI Coverage | 20% |
| Operational Readiness | 10% |

`DomainScore = sum(criterion_score * weight)`, де `criterion_score` у діапазоні `0..100`.

## Рівень 2 — загальний backend score

| Стовп | Вага |
|---|---:|
| Business domains (середнє по доменах) | 50% |
| Infrastructure & platform services | 15% |
| External integrations | 10% |
| Security & compliance (наскрізний) | 10% |
| Testing & quality gates (наскрізний) | 10% |
| DevOps / CI-CD / deploy | 5% |

`OverallScore = sum(pillar_score * pillar_weight)`.

## Статуси готовності

| Статус | Умова |
|---|---|
| **Ready** | Score ≥ 90, blockers = 0 |
| **Conditional** | Score 75–89 або є P1 blockers з mitigation plan |
| **Not ready** | Score < 75 або є P0 blockers |

## Статуси доменів (реалізація)

- `Implemented`: повний вертикальний зріз (Domain + Application + API + Infrastructure).
- `Partial`: частина шарів без production-критичних елементів.
- `Conceptual`: лише модель/документація без робочого потоку.

## Confidence (впевненість у оцінці)

- **High**: є прогін тестів + code review + CI gate.
- **Medium**: code review + часткові тести.
- **Low**: лише статичний огляд коду.

## Блокери та типи робіт

- **Blockers (P0)**: без цього реліз у прод не рекомендується.
- **Near-term (P1)**: обов'язкові доробки найближчого спринту.
- **Optional (P2)**: зниження довгострокового ризику.

## Шаблон звіту (єдиний для всіх `*.md`)

```markdown
# [Назва]

- Статус реалізації: `Implemented` | `Partial` | `Conceptual`
- Готовність: **NN/100**
- Release status: `Ready` | `Conditional` | `Not ready`
- Confidence: `High` | `Medium` | `Low`
- Дата аудиту: YYYY-MM-DD

## Evidence
- Команда / файл: результат (exit code, дата)

## Що готово
- ...

## Blockers (P0)
- ...

## Near-term (P1)
- ...

## Optional (P2)
- ...

## Checklist
- [x] або [ ] пункт
```

## Формат Evidence

Артефакти зберігаються в `reports/production-readiness/evidence/`:

- `build-release.log` — `dotnet build -c Release`
- `test-*.log` — вихід `dotnet test`
- `vulnerabilities.txt` — `check_vulnerabilities.py` / `dotnet list package --vulnerable`
- `coverage-matrix-summary.md` — агрегація з `ENDPOINT_COVERAGE_MATRIX.md`

У звітах посилатися як: `Evidence: evidence/build-release.log (exit 0, 2026-06-29)`.

## Джерела даних

- Код: `backend/src/Marketplace.{Domain,Application,API,Infrastructure}`
- Тести: `backend/tests/`, `backend/tests/ENDPOINT_COVERAGE_MATRIX.md`
- CI: `.github/workflows/backend-ci.yml`
- Конфіг: `backend/.env.example`, `appsettings.json`, `docker-compose*.yml`
- Попередні звіти: `reports/production-readiness/domain-*.md`
