using Marketplace.Domain.Coupons.Entities;

namespace Marketplace.Application.Coupons.Validation;

public sealed record CouponRuleContext(
    Coupon Coupon,
    Guid ActorUserId,
    IReadOnlyList<CouponCartLine> Lines,
    IReadOnlyList<CouponCartLine> EligibleLines,
    int UserUsageCount,
    DateTime UtcNow);

public sealed record CouponRuleFailure(string ErrorCode, string Message);

public interface ICouponRule
{
    Task<CouponRuleFailure?> EvaluateAsync(CouponRuleContext context, CancellationToken ct = default);
}
