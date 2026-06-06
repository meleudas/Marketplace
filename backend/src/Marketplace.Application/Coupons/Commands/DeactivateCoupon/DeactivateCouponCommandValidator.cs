using FluentValidation;

namespace Marketplace.Application.Coupons.Commands.DeactivateCoupon;

public sealed class DeactivateCouponCommandValidator : AbstractValidator<DeactivateCouponCommand>
{
    public DeactivateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId).GreaterThan(0);
    }
}
