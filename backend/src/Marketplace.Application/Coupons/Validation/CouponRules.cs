namespace Marketplace.Application.Coupons.Validation;

public sealed class ActiveWindowCouponRule : ICouponRule
{
    public Task<CouponRuleFailure?> EvaluateAsync(CouponRuleContext context, CancellationToken ct = default)
    {
        if (context.Coupon.IsValidAt(context.UtcNow))
            return Task.FromResult<CouponRuleFailure?>(null);

        return Task.FromResult<CouponRuleFailure?>(new CouponRuleFailure("unprocessable", "Coupon is not active"));
    }
}

public sealed class CompanyScopeCouponRule : ICouponRule
{
    public Task<CouponRuleFailure?> EvaluateAsync(CouponRuleContext context, CancellationToken ct = default)
    {
        if (context.EligibleLines.Count > 0)
            return Task.FromResult<CouponRuleFailure?>(null);

        return Task.FromResult<CouponRuleFailure?>(new CouponRuleFailure("forbidden", "Coupon does not apply to cart items"));
    }
}

public sealed class UsageLimitsCouponRule : ICouponRule
{
    public Task<CouponRuleFailure?> EvaluateAsync(CouponRuleContext context, CancellationToken ct = default)
    {
        if (context.Coupon.IsUsageAvailableFor(context.ActorUserId, context.UserUsageCount))
            return Task.FromResult<CouponRuleFailure?>(null);

        return Task.FromResult<CouponRuleFailure?>(new CouponRuleFailure("conflict", "Coupon usage limit reached"));
    }
}

public sealed class MinOrderAmountCouponRule : ICouponRule
{
    public Task<CouponRuleFailure?> EvaluateAsync(CouponRuleContext context, CancellationToken ct = default)
    {
        var eligibleSubtotal = context.EligibleLines.Sum(x => x.LineTotal);
        if (context.Coupon.IsEligibleForSubtotal(new Domain.Common.ValueObjects.Money(eligibleSubtotal)))
            return Task.FromResult<CouponRuleFailure?>(null);

        return Task.FromResult<CouponRuleFailure?>(new CouponRuleFailure("unprocessable", "Cart does not satisfy min order amount"));
    }
}
