using Marketplace.Domain.Coupons;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Coupons.Validation;

public static class CouponJsonValidation
{
    public static bool IsValidScopeJson(string? json) =>
        CouponApplicableScope.IsValidJsonArray(string.IsNullOrWhiteSpace(json) ? null : new JsonBlob(json));

    public static bool IsValidDiscountType(string discountType, decimal discountAmount) =>
        !string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase) || discountAmount <= 100m;
}
