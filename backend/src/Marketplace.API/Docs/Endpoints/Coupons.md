# Coupons endpoints

## Admin coupons (`/admin/coupons`)

### `POST /admin/coupons`
- **Призначення:** створює купон.
- **Body:** `code`, `discountAmount`, `discountType`, `usageLimit`, `userUsageLimit`, `startsAtUtc`, `expiresAtUtc`, `applicableCompaniesJson`.
- **Повертає:** `CouponDto`.
- **Авторизація:** `Admin`, `Moderator`.
- **Помилки:** `409` (дублікат коду), `422` (валідація), `503` (feature disabled).

### `PATCH /admin/coupons/{id}`
- **Призначення:** оновлює параметри купона.
- **Повертає:** `CouponDto`.
- **Помилки:** `404`, `422`, `503`.

### `POST /admin/coupons/{id}/deactivate`
- **Призначення:** аварійне вимкнення купона.
- **Повертає:** `200 OK`.
- **Помилки:** `404`, `503`.

### `GET /admin/coupons/{id}/usage`
- **Призначення:** usage-звіт по купону.
- **Повертає:** `CouponUsageReportDto`.
- **Помилки:** `404`, `503`.

## Buyer cart coupons (`/me/cart/coupons`)

### `POST /me/cart/coupons/validate`
- **Призначення:** перевіряє застосовність купона для поточного кошика.
- **Body:** `code`.
- **Повертає:** `CouponValidationResultDto`.

### `POST /me/cart/coupons/apply`
- **Призначення:** застосовує купон до активного кошика.
- **Body:** `code`.
- **Повертає:** `CartCouponDto`.

### `DELETE /me/cart/coupons/{code}`
- **Призначення:** знімає купон з активного кошика.
- **Повертає:** `200 OK`.

## Checkout integration

- `POST /me/cart/checkout` автоматично враховує discount із `cart_coupon_links`.
- Consume виконується ідемпотентно через `coupon_usages` (`couponId + orderId` unique).
