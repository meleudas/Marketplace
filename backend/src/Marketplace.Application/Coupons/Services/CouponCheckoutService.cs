using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;
using Microsoft.Extensions.Options;
using Marketplace.Application.Coupons.Options;

namespace Marketplace.Application.Coupons.Services;

public sealed class CouponCheckoutService : ICouponCheckoutService
{
    private readonly ICartCouponLinkRepository _cartCouponLinkRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ICouponUsageRepository _couponUsageRepository;
    private readonly CouponsOptions _options;

    public CouponCheckoutService(
        ICartCouponLinkRepository cartCouponLinkRepository,
        ICouponRepository couponRepository,
        ICouponUsageRepository couponUsageRepository,
        IOptions<CouponsOptions> options)
    {
        _cartCouponLinkRepository = cartCouponLinkRepository;
        _couponRepository = couponRepository;
        _couponUsageRepository = couponUsageRepository;
        _options = options.Value;
    }

    public async Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(Guid actorUserId, CartId cartId, CompanyId companyId, decimal subtotal, CancellationToken ct = default)
    {
        _ = actorUserId;
        if (!_options.CheckoutConsumeEnabled)
            return new CheckoutCouponDiscountResult(0, null, null);

        var link = await _cartCouponLinkRepository.GetByCartIdAsync(cartId, ct);
        if (link is null || link.IsExpired(DateTime.UtcNow))
            return new CheckoutCouponDiscountResult(0, null, null);

        var coupon = await _couponRepository.GetByIdAsync(link.CouponId, ct);
        if (coupon is null || !coupon.IsValidAt(DateTime.UtcNow) || !coupon.IsCompanyInScope(companyId.Value))
            return new CheckoutCouponDiscountResult(0, null, null);

        if (!coupon.IsEligibleForSubtotal(new Money(subtotal)))
            return new CheckoutCouponDiscountResult(0, null, null);

        var userUsageCount = await _couponUsageRepository.CountByCouponAndUserAsync(coupon.Id, actorUserId, ct);
        if (!coupon.IsUsageAvailableFor(actorUserId, userUsageCount))
            return new CheckoutCouponDiscountResult(0, null, null);

        var discountAmount = coupon.CalculateDiscount(new Money(subtotal)).Amount;
        return new CheckoutCouponDiscountResult(discountAmount, coupon.Id.Value, coupon.Code);
    }

    public async Task ConsumeAsync(Guid actorUserId, OrderId orderId, long couponId, string couponCode, decimal discountAmount, CancellationToken ct = default)
    {
        if (!_options.CheckoutConsumeEnabled || discountAmount <= 0)
            return;

        var couponIdVo = CouponId.From(couponId);
        var alreadyConsumed = await _couponUsageRepository.ExistsByCouponAndOrderAsync(couponIdVo, orderId, ct);
        if (alreadyConsumed)
            return;

        var now = DateTime.UtcNow;
        var usage = CouponUsage.Reconstitute(
            CouponUsageId.From(0),
            couponIdVo,
            actorUserId,
            orderId,
            couponCode,
            new Money(discountAmount),
            now,
            now,
            now,
            false,
            null);

        await _couponUsageRepository.AddAsync(usage, ct);

        var coupon = await _couponRepository.GetByIdAsync(couponIdVo, ct);
        if (coupon is null)
            return;

        coupon.IncrementUsage(now);
        await _couponRepository.UpdateAsync(coupon, ct);
    }
}
