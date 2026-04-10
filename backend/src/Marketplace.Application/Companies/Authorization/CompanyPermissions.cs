using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Application.Companies.Authorization;

public static class CompanyPermissions
{
    public static bool CanManageMembers(CompanyMembershipRole role) =>
        role is CompanyMembershipRole.Owner or CompanyMembershipRole.Manager;
}
