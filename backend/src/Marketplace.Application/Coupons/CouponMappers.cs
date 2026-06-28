using Marketplace.Application.Coupons.DTOs;
using Marketplace.Domain.Coupons.Entities;

namespace Marketplace.Application.Coupons;

public static class CouponMappers
{
    public static CouponDto ToDto(this Coupon coupon) =>
        new(
            coupon.Id.Value,
            coupon.Code,
            coupon.Description,
            coupon.Discount.Amount,
            coupon.DiscountType.ToString(),
            coupon.MinOrderAmount?.Amount,
            coupon.UsageLimit,
            coupon.UsageCount,
            coupon.UserUsageLimit,
            coupon.StartsAt,
            coupon.ExpiresAt,
            coupon.IsActive);

    public static CartCouponDto ToDto(this CartCouponLink link) =>
        new(
            link.CartId.Value,
            link.CouponId.Value,
            link.CouponCode,
            link.AppliedAtUtc,
            link.ExpiresAtUtc);
}
