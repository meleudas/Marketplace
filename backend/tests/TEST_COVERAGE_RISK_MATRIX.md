# Test Coverage Risk Matrix

This matrix defines must-have unit tests for high-risk backend areas.

## Critical Modules

- `Application/Orders`
  - status transitions, cancel flow, idempotency replay, cache invalidation behavior
- `Application/Payments`
  - webhook signature/validation, monotonic status updates, retry-safe transitions
- `Application/Inventory`
  - reserve/release/ship/transfer concurrency conflicts and anti-oversell checks
- `Infrastructure/Jobs`
  - outbox dispatch retry, dead-letter, requeue and non-overlapping execution

## Coverage Governance

- Phase-1 global line coverage threshold (hard gate): `8%` (current baseline).
- Target thresholds for phase-increase:
  - `Marketplace.Application.Orders`: `75%`
  - `Marketplace.Application.Payments`: `75%`
  - `Marketplace.Application.Inventory`: `75%`
  - `Marketplace.Infrastructure.Jobs`: `70%`

## PR Expectations

- Any PR that changes critical modules must include/adjust tests in the same scope.
- A failing test in any critical module blocks merge.
- Coverage gate must pass before release merge.
