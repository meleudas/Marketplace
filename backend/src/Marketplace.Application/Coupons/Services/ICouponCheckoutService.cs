using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Coupons.Services;

public interface ICouponCheckoutService
{
    Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(Guid actorUserId, CartId cartId, CompanyId companyId, decimal subtotal, CancellationToken ct = default);
    Task ConsumeAsync(Guid actorUserId, OrderId orderId, long couponId, string couponCode, decimal discountAmount, CancellationToken ct = default);
}

public sealed record CheckoutCouponDiscountResult(
    decimal DiscountAmount,
    long? CouponId,
    string? CouponCode);
