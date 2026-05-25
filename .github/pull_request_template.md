## Summary

- What changed and why.

## Test Plan

- [ ] `api-regression`
- [ ] `contract-compat`
- [ ] `security-regression`
- [ ] `performance-baseline`
- [ ] `unit-coverage-gate`

## Security Checklist (required for payments/orders/inventory changes)

- [ ] AuthN/AuthZ paths reviewed (no privilege escalation).
- [ ] Input validation and error paths reviewed.
- [ ] Idempotency behavior verified for write endpoints.
- [ ] Webhook signature/anti-replay behavior verified.
- [ ] Dependency vulnerability policy checked.
