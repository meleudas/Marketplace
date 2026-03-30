using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;

public sealed class SendEmailTwoFactorCodeCommandValidator : AbstractValidator<SendEmailTwoFactorCodeCommand>
{
    public SendEmailTwoFactorCodeCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");
    }
}

