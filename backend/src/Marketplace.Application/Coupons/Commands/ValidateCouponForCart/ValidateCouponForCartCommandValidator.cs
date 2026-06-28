using FluentValidation;

namespace Marketplace.Application.Coupons.Commands.ValidateCouponForCart;

public sealed class ValidateCouponForCartCommandValidator : AbstractValidator<ValidateCouponForCartCommand>
{
    public ValidateCouponForCartCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
    }
}
