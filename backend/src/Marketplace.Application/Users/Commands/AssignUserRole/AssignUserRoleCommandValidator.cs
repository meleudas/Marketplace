using FluentValidation;

namespace Marketplace.Application.Users.Commands.AssignUserRole;

public sealed class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty()
            .WithMessage("Identity user id is required");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Role is invalid");
    }
}
