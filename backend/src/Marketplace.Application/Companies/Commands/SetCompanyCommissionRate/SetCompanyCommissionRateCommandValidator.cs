using FluentValidation;

namespace Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;

public sealed class SetCompanyCommissionRateCommandValidator : AbstractValidator<SetCompanyCommissionRateCommand>
{
    public SetCompanyCommissionRateCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.CommissionPercent)
            .GreaterThan(0m)
            .LessThanOrEqualTo(100m);
    }
}
