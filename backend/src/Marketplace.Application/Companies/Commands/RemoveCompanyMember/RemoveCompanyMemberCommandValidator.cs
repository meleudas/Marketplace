using FluentValidation;

namespace Marketplace.Application.Companies.Commands.RemoveCompanyMember;

public sealed class RemoveCompanyMemberCommandValidator : AbstractValidator<RemoveCompanyMemberCommand>
{
    public RemoveCompanyMemberCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
