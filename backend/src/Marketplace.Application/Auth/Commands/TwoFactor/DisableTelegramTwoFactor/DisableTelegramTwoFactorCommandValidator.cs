using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableTelegramTwoFactor;

public sealed class DisableTelegramTwoFactorCommandValidator : AbstractValidator<DisableTelegramTwoFactorCommand>
{
    public DisableTelegramTwoFactorCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");
    }
}
