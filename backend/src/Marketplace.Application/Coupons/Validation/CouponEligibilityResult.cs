namespace Marketplace.Application.Coupons.Validation;

public sealed record CouponEligibilityResult(
    bool IsValid,
    string? ErrorCode,
    string? Message,
    decimal CartSubtotal,
    decimal EligibleSubtotal,
    decimal DiscountAmount,
    IReadOnlyDictionary<Guid, decimal> EligibleSubtotalByCompany)
{
    public static CouponEligibilityResult Invalid(
        string errorCode,
        string message,
        decimal cartSubtotal) =>
        new(false, errorCode, message, cartSubtotal, 0, 0, new Dictionary<Guid, decimal>());

    public static CouponEligibilityResult Valid(
        decimal cartSubtotal,
        decimal eligibleSubtotal,
        decimal discountAmount,
        IReadOnlyDictionary<Guid, decimal> eligibleSubtotalByCompany) =>
        new(true, null, null, cartSubtotal, eligibleSubtotal, discountAmount, eligibleSubtotalByCompany);
}
