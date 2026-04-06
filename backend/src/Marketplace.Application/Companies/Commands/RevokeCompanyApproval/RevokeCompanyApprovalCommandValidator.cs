using FluentValidation;

namespace Marketplace.Application.Companies.Commands.RevokeCompanyApproval;

public sealed class RevokeCompanyApprovalCommandValidator : AbstractValidator<RevokeCompanyApprovalCommand>
{
    public RevokeCompanyApprovalCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company id is required");
    }
}
