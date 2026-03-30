using FluentValidation;

namespace Marketplace.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .MaximumLength(2048)
            .WithMessage("Refresh token is too long")
            .When(x => !string.IsNullOrWhiteSpace(x.RefreshToken));
    }
}

