using FluentValidation;
using Marketplace.Application.Coupons.Validation;

namespace Marketplace.Application.Coupons.Commands.UpdateCoupon;

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId).GreaterThan(0);
        RuleFor(x => x.DiscountAmount).GreaterThan(0);
        RuleFor(x => x.UserUsageLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UsageLimit).GreaterThan(0).When(x => x.UsageLimit.HasValue);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x).Must(x => !x.StartsAtUtc.HasValue || !x.ExpiresAtUtc.HasValue || x.StartsAtUtc <= x.ExpiresAtUtc)
            .WithMessage("startsAtUtc must be less than or equal to expiresAtUtc");
        RuleFor(x => x).Must(x => CouponJsonValidation.IsValidDiscountType(x.DiscountType, x.DiscountAmount))
            .WithMessage("Percentage discount cannot exceed 100");
        RuleFor(x => x.ApplicableCompaniesJson).Must(CouponJsonValidation.IsValidScopeJson)
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableCompaniesJson))
            .WithMessage("applicableCompaniesJson must be a JSON array");
        RuleFor(x => x.ApplicableCategoriesJson).Must(CouponJsonValidation.IsValidScopeJson)
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableCategoriesJson))
            .WithMessage("applicableCategoriesJson must be a JSON array");
        RuleFor(x => x.ApplicableProductsJson).Must(CouponJsonValidation.IsValidScopeJson)
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableProductsJson))
            .WithMessage("applicableProductsJson must be a JSON array");
    }
}
