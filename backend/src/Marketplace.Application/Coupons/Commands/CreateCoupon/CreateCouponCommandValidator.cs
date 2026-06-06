using FluentValidation;

namespace Marketplace.Application.Coupons.Commands.CreateCoupon;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DiscountAmount).GreaterThan(0);
        RuleFor(x => x.UserUsageLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UsageLimit).GreaterThan(0).When(x => x.UsageLimit.HasValue);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x).Must(x => !x.StartsAtUtc.HasValue || !x.ExpiresAtUtc.HasValue || x.StartsAtUtc <= x.ExpiresAtUtc)
            .WithMessage("startsAtUtc must be less than expiresAtUtc");
    }
}
