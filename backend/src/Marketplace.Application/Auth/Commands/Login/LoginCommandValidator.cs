using FluentValidation;


namespace Marketplace.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            RuleFor(x => x.TwoFactorCode)
                .Matches(@"^\d{6}$").WithMessage("Two-factor code must contain 6 digits")
                .When(x => !string.IsNullOrWhiteSpace(x.TwoFactorCode));
        }
    }
}
