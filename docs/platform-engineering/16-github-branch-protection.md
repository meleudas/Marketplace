# 16 — GitHub branch protection

Checklist для власника репозиторію (потребує admin прав, не автоматизується в коді).

## Рекомендовані required checks (branch `main`)

| Check | Джерело |
|-------|---------|
| `integration-full-main` | [backend-ci.yml](../../.github/workflows/backend-ci.yml) |
| `unit-coverage-gate` | Global line coverage ≥ 10% |
| Domain gates (P0) | `cart-checkout-gate`, `payments-gate`, `orders-gate`, `identity-access-gate` |

## GitHub CLI (приклад)

```bash
gh api repos/{owner}/{repo}/branches/main/protection \
  --method PUT \
  --field required_status_checks[strict]=true \
  --field required_status_checks[contexts][]=integration-full-main \
  --field enforce_admins=false \
  --field required_pull_request_reviews[required_approving_review_count]=1 \
  --field restrictions=null
```

## UI checklist

- [ ] Settings → Branches → Add rule for `main`
- [ ] Require pull request before merging
- [ ] Require status checks: `integration-full-main`
- [ ] Require branches to be up to date
- [ ] Do not allow bypassing (optional for admins)

## Підтвердження

Відмітити в [evidence/p1-completion-log.md](../../reports/production-readiness/evidence/p1-completion-log.md) після застосування налаштувань.
