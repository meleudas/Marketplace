using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;

public sealed class DisableEmailTwoFactorCommandValidator : AbstractValidator<DisableEmailTwoFactorCommand>
{
    public DisableEmailTwoFactorCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");
    }
}

