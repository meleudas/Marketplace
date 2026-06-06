using FluentValidation;

namespace Marketplace.Application.Coupons.Commands.ApplyCouponToCart;

public sealed class ApplyCouponToCartCommandValidator : AbstractValidator<ApplyCouponToCartCommand>
{
    public ApplyCouponToCartCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
    }
}
