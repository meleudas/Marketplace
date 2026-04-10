using FluentValidation;

namespace Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;

public sealed class ChangeCompanyMemberRoleCommandValidator : AbstractValidator<ChangeCompanyMemberRoleCommand>
{
    public ChangeCompanyMemberRoleCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
