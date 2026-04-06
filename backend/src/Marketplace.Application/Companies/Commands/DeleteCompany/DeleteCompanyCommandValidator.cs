using FluentValidation;

namespace Marketplace.Application.Companies.Commands.DeleteCompany;

public sealed class DeleteCompanyCommandValidator : AbstractValidator<DeleteCompanyCommand>
{
    public DeleteCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company id is required");
    }
}
