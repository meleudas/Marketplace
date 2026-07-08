using FluentValidation;
using Marketplace.Application.Auth.Validation;


namespace Marketplace.Application.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain number")
                .Matches(@"[\W_]").WithMessage("Password must contain special character");

            RuleFor(x => x.UserName)
                .ApplyUserNameRules();

            RuleFor(x => x.PhoneNumber)
                .ApplyOptionalPhoneNumberRules(x => x.PhoneNumber);
        }
    }
}
