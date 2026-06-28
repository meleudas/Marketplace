# 02 - Coupons Production Spec

## 1. Context and business goals

Coupons впливають на конверсію checkout і середній чек. Ціль - безпечна знижкова модель без фінансових витоків і race-condition у пікові періоди.

## 2. Domain target state

Наявні базові сутності:

- `Marketplace.Domain/Coupons/Entities/Coupon.cs`
- `Marketplace.Domain/Coupons/Entities/OrderCoupon.cs`

Потрібно додати/закріпити:

- інваріанти:
  - code унікальний (case-insensitive);
  - `startsAt <= now <= expiresAt` для активного застосування;
  - usage не перевищує `usageLimit` і `userUsageLimit`;
  - compatibility rule (stacking policy).
- value objects:
  - `CouponCode`
  - `CouponScope` (products/categories/companies)
  - `DiscountPolicy` (fixed/percent, caps).

## 3. Application target state

### Команди/запити

- `CreateCoupon`, `UpdateCoupon`, `DeactivateCoupon`
- `ValidateCouponForCart`
- `ApplyCouponToCart`, `RemoveCouponFromCart`
- `ConsumeCouponOnCheckout`
- `GetCouponUsageReport`

### Політики

- Валідація купона виконується і до checkout, і в момент checkout (recheck).
- Ідемпотентність `ConsumeCouponOnCheckout` обов'язкова.
- Всі write операції купонів з audit trail.

## 4. API target state

### Endpoints

- `POST /admin/coupons`
- `PATCH /admin/coupons/{id}`
- `POST /admin/coupons/{id}/deactivate`
- `POST /me/cart/coupons/validate`
- `POST /me/cart/coupons/apply`
- `DELETE /me/cart/coupons/{code}`
- `GET /admin/coupons/{id}/usage`

### Authz

- Admin/Finance: create/update/deactivate/report.
- Buyer: validate/apply/remove only for own cart.

### Error model

- `409` usage exhausted / compatibility conflict.
- `422` invalid scope/min order amount mismatch.
- `403` admin-only endpoints.

## 5. Infrastructure and data model

### Таблиці

- `coupons` (code, status, limits, scope json, schedule)
- `coupon_usages` (coupon_id, user_id, order_id, consumed_at)
- `cart_coupon_links` (cart_id, coupon_id, validation_snapshot)

### Індекси/обмеження

- unique index on normalized coupon code.
- unique `(coupon_id, order_id)` для idempotent consume.
- partial index для активних купонів за часовими межами.

## 6. Security and abuse resistance

- Rate limit на validate/apply endpoint.
- Захист від brute-force code enumeration:
  - sliding window throttling;
  - generic error messages для публічних помилок.
- Fraud controls:
  - velocity checks per user/device/IP;
  - anomaly signal у telemetry.

## 7. Testing strategy and CI gates

### Unit (Suite=Coupons)

- валідність періоду, usage limits, discount math;
- stacking compatibility;
- min order threshold.

### IntegrationLight

- apply/remove coupon to cart;
- checkout consume з повторним викликом (idempotent).

### IntegrationContainers

- паралельне споживання купонів під навантаженням;
- dedup/unique index behavior в Postgres.

### E2E

- buyer: validate -> apply -> checkout (discount reflected in order).
- exhausted/expired coupon показує очікувану помилку.

### CI gate

- `coupons-gate`:
  - Unit + IntegrationLight (`Suite=Coupons`)
  - Coverage threshold >= 12%
- full suite для `integration-full`.

## 8. Observability and runbook

### Метрики

- `coupon_operations_total`
- `coupon_errors_total`
- `coupon_validation_failures_total`
- `coupon_abuse_signals_total`

### Алерти

- різкий ріст validation failures.
- usage spikes по одному code.

### Runbook

- emergency deactivate coupon.
- reconciliation report: coupon discounts vs orders.

## 9. Release and rollback strategy

- feature flags:
  - `CouponsReadEnabled`
  - `CouponsCheckoutConsumeEnabled`
- rollout:
  1. admin management;
  2. read/validate;
  3. apply + checkout consume.
- rollback: disable consume flag, keep audit/usage consistency.

## 10. Definition of Done (100/100)

- [ ] Купонні правила формалізовані як інваріанти, не в контролерах.
- [ ] Checkout має повторну перевірку купона і idempotent consume.
- [ ] Є anti-abuse контроли + аудит.
- [ ] Є Unit/Integration/E2E покриття та `Suite=Coupons` gate.
- [ ] Є метрики, алерти, runbook і emergency deactivate path.
