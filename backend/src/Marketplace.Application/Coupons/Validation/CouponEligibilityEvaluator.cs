using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;

namespace Marketplace.Application.Coupons.Validation;

public sealed class CouponEligibilityEvaluator
{
    private readonly ICouponUsageRepository _couponUsageRepository;
    private readonly IReadOnlyList<ICouponRule> _rules;

    public CouponEligibilityEvaluator(
        ICouponUsageRepository couponUsageRepository,
        IEnumerable<ICouponRule> rules)
    {
        _couponUsageRepository = couponUsageRepository;
        _rules = rules.ToList();
    }

    public async Task<CouponEligibilityResult> EvaluateAsync(
        Coupon coupon,
        Guid actorUserId,
        IReadOnlyList<CouponCartLine> lines,
        CancellationToken ct = default)
    {
        var cartSubtotal = lines.Sum(x => x.LineTotal);
        var eligibleLines = lines.Where(x => coupon.IsLineInScope(x.CompanyId, x.CategoryId, x.ProductId)).ToList();
        var userUsageCount = await _couponUsageRepository.CountByCouponAndUserAsync(coupon.Id, actorUserId, ct);
        var context = new CouponRuleContext(coupon, actorUserId, lines, eligibleLines, userUsageCount, DateTime.UtcNow);

        foreach (var rule in _rules)
        {
            var failure = await rule.EvaluateAsync(context, ct);
            if (failure is not null)
                return CouponEligibilityResult.Invalid(failure.ErrorCode, failure.Message, cartSubtotal);
        }

        var eligibleSubtotal = eligibleLines.Sum(x => x.LineTotal);
        var discount = coupon.CalculateDiscount(new Money(eligibleSubtotal)).Amount;
        var byCompany = eligibleLines
            .GroupBy(x => x.CompanyId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.LineTotal));

        return CouponEligibilityResult.Valid(cartSubtotal, eligibleSubtotal, discount, byCompany);
    }

    public CouponCheckoutPlan BuildCheckoutPlan(CouponEligibilityResult eligibility)
    {
        if (!eligibility.IsValid || eligibility.DiscountAmount <= 0)
            return CouponCheckoutPlan.Empty;

        var discountByCompany = AllocateDiscount(eligibility.DiscountAmount, eligibility.EligibleSubtotalByCompany);
        return new CouponCheckoutPlan(
            eligibility.DiscountAmount,
            discountByCompany);
    }

    public static IReadOnlyDictionary<Guid, decimal> AllocateDiscount(
        decimal totalDiscount,
        IReadOnlyDictionary<Guid, decimal> eligibleSubtotalByCompany)
    {
        if (totalDiscount <= 0 || eligibleSubtotalByCompany.Count == 0)
            return new Dictionary<Guid, decimal>();

        var eligibleTotal = eligibleSubtotalByCompany.Values.Sum();
        if (eligibleTotal <= 0)
            return new Dictionary<Guid, decimal>();

        var result = new Dictionary<Guid, decimal>();
        decimal allocated = 0;
        var companies = eligibleSubtotalByCompany.Keys.ToList();
        for (var i = 0; i < companies.Count; i++)
        {
            var companyId = companies[i];
            var share = i == companies.Count - 1
                ? totalDiscount - allocated
                : Math.Round(totalDiscount * (eligibleSubtotalByCompany[companyId] / eligibleTotal), 2);
            if (share < 0)
                share = 0;
            result[companyId] = share;
            allocated += share;
        }

        return result;
    }
}

public sealed record CouponCheckoutPlan(
    decimal TotalDiscount,
    IReadOnlyDictionary<Guid, decimal> DiscountByCompanyId)
{
    public static CouponCheckoutPlan Empty { get; } = new(0, new Dictionary<Guid, decimal>());

    public decimal GetDiscountForCompany(Guid companyId) =>
        DiscountByCompanyId.TryGetValue(companyId, out var amount) ? amount : 0;
}
