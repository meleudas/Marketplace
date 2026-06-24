using Marketplace.Application.Coupons.Validation;
using Marketplace.Domain.Catalog.Entities;
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
    private readonly CouponCartValidationService _cartValidation;
    private readonly CouponEligibilityEvaluator _eligibility;
    private readonly CouponsOptions _options;

    public CouponCheckoutService(
        ICartCouponLinkRepository cartCouponLinkRepository,
        ICouponRepository couponRepository,
        ICouponUsageRepository couponUsageRepository,
        CouponCartValidationService cartValidation,
        CouponEligibilityEvaluator eligibility,
        IOptions<CouponsOptions> options)
    {
        _cartCouponLinkRepository = cartCouponLinkRepository;
        _couponRepository = couponRepository;
        _couponUsageRepository = couponUsageRepository;
        _cartValidation = cartValidation;
        _eligibility = eligibility;
        _options = options.Value;
    }

    public async Task<CheckoutCouponPlanResult> ResolveCheckoutPlanAsync(
        Guid actorUserId,
        CartId cartId,
        IReadOnlyList<CouponCartLine> lines,
        CancellationToken ct = default)
    {
        if (!_options.CheckoutConsumeEnabled)
            return CheckoutCouponPlanResult.Empty;

        var link = await _cartCouponLinkRepository.GetByCartIdAsync(cartId, ct);
        if (link is null || link.IsExpired(DateTime.UtcNow))
            return CheckoutCouponPlanResult.Empty;

        var coupon = await _couponRepository.GetByIdAsync(link.CouponId, ct);
        if (coupon is null)
            return CheckoutCouponPlanResult.Empty;

        var eligibility = await _eligibility.EvaluateAsync(coupon, actorUserId, lines, ct);
        if (!eligibility.IsValid)
        {
            return new CheckoutCouponPlanResult(
                false,
                eligibility.ErrorCode,
                eligibility.Message,
                null,
                null,
                CouponCheckoutPlan.Empty);
        }

        var plan = _eligibility.BuildCheckoutPlan(eligibility);
        return new CheckoutCouponPlanResult(
            true,
            null,
            null,
            coupon.Id.Value,
            coupon.Code,
            plan);
    }

    public async Task<CheckoutCouponRevalidationResult> RevalidateForCheckoutAsync(
        Guid actorUserId,
        CartId cartId,
        IReadOnlyList<Domain.Cart.Entities.CartItem> items,
        IReadOnlyDictionary<long, Product> productMap,
        CancellationToken ct = default)
    {
        var link = await _cartCouponLinkRepository.GetByCartIdAsync(cartId, ct);
        if (link is null || link.IsExpired(DateTime.UtcNow))
            return CheckoutCouponRevalidationResult.Valid();

        var coupon = await _couponRepository.GetByIdAsync(link.CouponId, ct);
        if (coupon is null)
        {
            await _cartCouponLinkRepository.RemoveByCartIdAsync(cartId, ct);
            return CheckoutCouponRevalidationResult.Invalid("not_found", "Coupon not found");
        }

        var eligibility = await _cartValidation.ValidateCartAsync(actorUserId, cartId, items, productMap, coupon, ct);
        if (!eligibility.IsValid)
        {
            await _cartCouponLinkRepository.RemoveByCartIdAsync(cartId, ct);
            return CheckoutCouponRevalidationResult.Invalid(eligibility.ErrorCode ?? "unprocessable", eligibility.Message ?? "Coupon is not valid");
        }

        return CheckoutCouponRevalidationResult.Valid();
    }

    public async Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(
        Guid actorUserId,
        CartId cartId,
        CompanyId companyId,
        decimal subtotal,
        CancellationToken ct = default)
    {
        _ = actorUserId;
        _ = subtotal;
        var lines = await _cartValidation.BuildLinesForCartAsync(cartId, ct);
        var planResult = await ResolveCheckoutPlanAsync(actorUserId, cartId, lines, ct);
        if (!planResult.IsValid || planResult.CouponId is null)
            return new CheckoutCouponDiscountResult(0, null, null);

        var discount = planResult.Plan.GetDiscountForCompany(companyId.Value);
        return new CheckoutCouponDiscountResult(discount, planResult.CouponId, planResult.CouponCode);
    }

    public async Task ConsumeOnceAsync(
        Guid actorUserId,
        OrderId primaryOrderId,
        CartId cartId,
        long couponId,
        string couponCode,
        decimal totalDiscountAmount,
        CancellationToken ct = default)
    {
        if (!_options.CheckoutConsumeEnabled || totalDiscountAmount <= 0)
            return;

        var couponIdVo = CouponId.From(couponId);
        var alreadyConsumed = await _couponUsageRepository.ExistsByCouponAndOrderAsync(couponIdVo, primaryOrderId, ct);
        if (alreadyConsumed)
            return;

        var now = DateTime.UtcNow;
        var usage = CouponUsage.Reconstitute(
            CouponUsageId.From(0),
            couponIdVo,
            actorUserId,
            primaryOrderId,
            couponCode,
            new Money(totalDiscountAmount),
            now,
            now,
            now,
            false,
            null);

        await _couponUsageRepository.AddAsync(usage, ct);

        var coupon = await _couponRepository.GetByIdAsync(couponIdVo, ct);
        if (coupon is not null)
        {
            coupon.IncrementUsage(now);
            await _couponRepository.UpdateAsync(coupon, ct);
        }

        await _cartCouponLinkRepository.RemoveByCartIdAsync(cartId, ct);
    }

    public async Task ConsumeAsync(
        Guid actorUserId,
        OrderId orderId,
        long couponId,
        string couponCode,
        decimal discountAmount,
        CancellationToken ct = default)
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
