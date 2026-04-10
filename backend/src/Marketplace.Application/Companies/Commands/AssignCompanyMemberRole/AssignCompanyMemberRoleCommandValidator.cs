using FluentValidation;

namespace Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;

public sealed class AssignCompanyMemberRoleCommandValidator : AbstractValidator<AssignCompanyMemberRoleCommand>
{
    public AssignCompanyMemberRoleCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
