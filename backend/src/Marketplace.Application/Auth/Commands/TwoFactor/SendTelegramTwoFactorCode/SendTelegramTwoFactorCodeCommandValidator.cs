using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendTelegramTwoFactorCode;

public sealed class SendTelegramTwoFactorCodeCommandValidator : AbstractValidator<SendTelegramTwoFactorCodeCommand>
{
    public SendTelegramTwoFactorCodeCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");
    }
}
