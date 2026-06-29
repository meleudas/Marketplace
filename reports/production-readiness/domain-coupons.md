# Domain Report: Coupons

- Статус реалізації: `Implemented`
- Готовність: **83/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `coupons-gate`
- Integration Light: IntegrationCouponsSqliteTests
- Container: CouponEligibilityPostgresTests (validate/apply/remove)

## Межі домену

- `backend/src/Marketplace.Domain/Coupons`
- `backend/src/Marketplace.Application/Coupons`
- Cart coupon endpoints, `AdminCouponsController`

## Що готово

- CouponEligibilityEvaluator з rules (window, scope, usage, min order).
- Apply/validate/consume на checkout.
- Feature flags `Coupons:ReadEnabled`, `CheckoutConsumeEnabled`.

## Blockers (P0)

- Немає.

## Near-term (P1)

- Container test для admin coupon CRUD.
- Anti-abuse на brute-force validate.

## Checklist

- [x] Validate/apply у cart
- [x] coupons-gate CI
- [x] Container eligibility flow
- [ ] Admin coupon lifecycle container test
