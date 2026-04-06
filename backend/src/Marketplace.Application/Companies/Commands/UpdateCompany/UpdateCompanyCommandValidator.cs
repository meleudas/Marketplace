using FluentValidation;

namespace Marketplace.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("Company id is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Company name is required");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Company slug is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Company description is required");

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Company contact email is required");

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Company contact phone is required");

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Company address is required");
    }
}
