using FluentValidation;

namespace Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;

public sealed class RemoveCouponFromCartCommandValidator : AbstractValidator<RemoveCouponFromCartCommand>
{
    public RemoveCouponFromCartCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
    }
}
