using FluentValidation;

namespace Marketplace.Application.Companies.Commands.ApproveCompany;

public sealed class ApproveCompanyCommandValidator : AbstractValidator<ApproveCompanyCommand>
{
    public ApproveCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company id is required");

        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Admin user id is required");
    }
}
