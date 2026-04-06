using FluentValidation;

namespace Marketplace.Application.Auth.Queries.GetTwoFactorStatus;

public sealed class GetTwoFactorStatusQueryValidator : AbstractValidator<GetTwoFactorStatusQuery>
{
    public GetTwoFactorStatusQueryValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty()
            .WithMessage("Identity user id is required");
    }
}
