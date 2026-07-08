using FluentValidation;
using System;

namespace Marketplace.Application.Auth.Validation;

public static class UserIdentityValidationRules
{
    public static IRuleBuilderOptions<T, string> ApplyUserNameRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");
    }

    public static IRuleBuilderOptions<T, string?> ApplyOptionalPhoneNumberRules<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        Func<T, string?> valueAccessor)
    {
        return ruleBuilder
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrWhiteSpace(valueAccessor(x)));
    }
}
