using FluentValidation;

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
            .WithMessage("startsAtUtc must be less than expiresAtUtc");
    }
}
