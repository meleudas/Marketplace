using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;

public sealed class EnableTelegramTwoFactorCommandValidator : AbstractValidator<EnableTelegramTwoFactorCommand>
{
    public EnableTelegramTwoFactorCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Two-factor code is required")
            .Matches(@"^\d{6}$").WithMessage("Two-factor code must contain 6 digits");
    }
}
