# Domain Report: Returns (RMA)

- Статус реалізації: `Implemented`
- Готовність: **86/100**
- Release status: `Conditional`
- Confidence: `Medium`
- Дата аудиту: 2026-06-29

## Evidence

- Container: ReturnRequestWorkflowPostgresTests
- E2E: SeedCouponReturnChatE2ETests (`GET /me/returns`)
- Unit: returns handlers у Application layer

## Межі домену

- `backend/src/Marketplace.Domain/Returns`
- `backend/src/Marketplace.Application/Returns`
- `ReturnsController`, `ReturnRequestRepository`

## Що готово

- Return request workflow (approve, received, refund paths).
- Company/buyer list queries.

## Blockers (P0)

- Немає.

## Near-term (P1)

- Повний container test refund→ledger integration.
- E2E create return → approve → refund.

## Checklist

- [x] Domain workflow entities
- [x] API endpoints
- [x] Partial container workflow test
- [ ] End-to-end refund financial reconciliation test
