using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Tests;

public class DomainCompanyMemberTests
{
    [Fact]
    public void Create_Sets_IsOwner_For_Owner_Role()
    {
        var member = CompanyMember.Create(CompanyId.From(Guid.NewGuid()), Guid.NewGuid(), CompanyMembershipRole.Owner);
        Assert.True(member.IsOwner);
    }

    [Fact]
    public void ChangeRole_Updates_IsOwner()
    {
        var member = CompanyMember.Create(CompanyId.From(Guid.NewGuid()), Guid.NewGuid(), CompanyMembershipRole.Seller);
        member.ChangeRole(CompanyMembershipRole.Owner);
        Assert.True(member.IsOwner);
    }
}
