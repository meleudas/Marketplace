using FluentValidation;

namespace Marketplace.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
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

        RuleFor(x => x.LegalProfile)
            .NotNull()
            .WithMessage("Company legal profile is required");

        RuleFor(x => x.LegalProfile.LegalName)
            .NotEmpty()
            .WithMessage("Legal name is required");

        RuleFor(x => x.LegalProfile.LegalType)
            .NotEmpty()
            .WithMessage("Legal type is required");

        RuleFor(x => x.LegalProfile.InitialCommissionPercent)
            .InclusiveBetween(0.0001m, 100m)
            .When(x => x.LegalProfile.InitialCommissionPercent.HasValue)
            .WithMessage("Initial commission percent must be in range (0, 100]");

        RuleFor(x => x.LegalProfile.Edrpou)
            .NotEmpty()
            .When(x => x.LegalProfile.LegalType is "llc" or "jsc")
            .WithMessage("EDRPOU is required for legal entities");

        RuleFor(x => x.LegalProfile.Ipn)
            .NotEmpty()
            .When(x => x.LegalProfile.LegalType is "individual" or "entrepreneur")
            .WithMessage("IPN is required for individual or entrepreneur");
    }
}
