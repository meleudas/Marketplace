using FluentValidation;

namespace Marketplace.Application.Auth.Commands.TwoFactor.LinkTelegramAccount;

public sealed class LinkTelegramAccountCommandValidator : AbstractValidator<LinkTelegramAccountCommand>
{
    public LinkTelegramAccountCommandValidator()
    {
        RuleFor(x => x.LinkCode)
            .NotEmpty().WithMessage("Link code is required")
            .Length(6, 32).WithMessage("Link code has invalid length");

        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("Chat id is required")
            .MaximumLength(64).WithMessage("Chat id is too long");
    }
}
