using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;

namespace Marketplace.Tests;

[Trait("Suite", "CompaniesWorkspace")]
public class ApplicationCompanyWorkspaceValidatorTests
{
    [Fact]
    public void AssignCompanyMemberRoleValidator_Rejects_Empty_Ids()
    {
        var validator = new AssignCompanyMemberRoleCommandValidator();
        var result = validator.Validate(new AssignCompanyMemberRoleCommand(Guid.Empty, Guid.Empty, 0, Guid.Empty, false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CompanyId");
        Assert.Contains(result.Errors, e => e.PropertyName == "TargetUserId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }

    [Fact]
    public void ChangeCompanyMemberRoleValidator_Rejects_Empty_Ids()
    {
        var validator = new ChangeCompanyMemberRoleCommandValidator();
        var result = validator.Validate(new ChangeCompanyMemberRoleCommand(Guid.Empty, Guid.Empty, 0, Guid.Empty, false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CompanyId");
        Assert.Contains(result.Errors, e => e.PropertyName == "TargetUserId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }

    [Fact]
    public void RemoveCompanyMemberValidator_Rejects_Empty_Ids()
    {
        var validator = new RemoveCompanyMemberCommandValidator();
        var result = validator.Validate(new RemoveCompanyMemberCommand(Guid.Empty, Guid.Empty, Guid.Empty, false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CompanyId");
        Assert.Contains(result.Errors, e => e.PropertyName == "TargetUserId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }
}
