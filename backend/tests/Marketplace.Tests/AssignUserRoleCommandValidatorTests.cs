using Marketplace.Application.Users.Commands.AssignUserRole;
using Marketplace.Domain.Users.Enums;

namespace Marketplace.Tests;

public class AssignUserRoleCommandValidatorTests
{
    [Fact]
    public void Validator_Rejects_Empty_UserId()
    {
        var validator = new AssignUserRoleCommandValidator();
        var command = new AssignUserRoleCommand(Guid.Empty, UserRole.Admin);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IdentityUserId");
    }

    [Fact]
    public void Validator_Allows_Valid_Command()
    {
        var validator = new AssignUserRoleCommandValidator();
        var command = new AssignUserRoleCommand(Guid.NewGuid(), UserRole.Admin);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
