using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.GenerateTelegramLinkCode;

public sealed class GenerateTelegramLinkCodeCommandValidator : AbstractValidator<GenerateTelegramLinkCodeCommand>
{
    public GenerateTelegramLinkCodeCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("Identity user id is required");
    }
}
