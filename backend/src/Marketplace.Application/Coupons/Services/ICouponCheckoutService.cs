using Marketplace.Application.Coupons.Validation;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Coupons.Services;

public interface ICouponCheckoutService
{
    Task<CheckoutCouponPlanResult> ResolveCheckoutPlanAsync(
        Guid actorUserId,
        CartId cartId,
        IReadOnlyList<CouponCartLine> lines,
        CancellationToken ct = default);

    Task<CheckoutCouponRevalidationResult> RevalidateForCheckoutAsync(
        Guid actorUserId,
        CartId cartId,
        IReadOnlyList<Domain.Cart.Entities.CartItem> items,
        IReadOnlyDictionary<long, Domain.Catalog.Entities.Product> productMap,
        CancellationToken ct = default);

    Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(
        Guid actorUserId,
        CartId cartId,
        CompanyId companyId,
        decimal subtotal,
        CancellationToken ct = default);

    Task ConsumeOnceAsync(
        Guid actorUserId,
        OrderId primaryOrderId,
        CartId cartId,
        long couponId,
        string couponCode,
        decimal totalDiscountAmount,
        CancellationToken ct = default);

    Task ConsumeAsync(
        Guid actorUserId,
        OrderId orderId,
        long couponId,
        string couponCode,
        decimal discountAmount,
        CancellationToken ct = default);
}

public sealed record CheckoutCouponDiscountResult(
    decimal DiscountAmount,
    long? CouponId,
    string? CouponCode);

public sealed record CheckoutCouponPlanResult(
    bool IsValid,
    string? ErrorCode,
    string? Message,
    long? CouponId,
    string? CouponCode,
    CouponCheckoutPlan Plan)
{
    public static CheckoutCouponPlanResult Empty { get; } = new(true, null, null, null, null, CouponCheckoutPlan.Empty);
}

public sealed record CheckoutCouponRevalidationResult(bool IsValid, string? ErrorCode, string? Message)
{
    public static CheckoutCouponRevalidationResult Valid() => new(true, null, null);

    public static CheckoutCouponRevalidationResult Invalid(string errorCode, string message) =>
        new(false, errorCode, message);
}
