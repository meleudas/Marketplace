using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Application.Finance.Authorization;

public enum FinancePermission
{
    Read,
    ManagePayoutProfile
}

public interface IFinanceAccessService
{
    Task<bool> HasAccessAsync(
        Guid companyId,
        Guid actorUserId,
        bool isActorAdmin,
        FinancePermission permission,
        CancellationToken ct = default);
}

public sealed class FinanceAccessService : IFinanceAccessService
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public FinanceAccessService(ICompanyMemberRepository companyMemberRepository) =>
        _companyMemberRepository = companyMemberRepository;

    public async Task<bool> HasAccessAsync(
        Guid companyId,
        Guid actorUserId,
        bool isActorAdmin,
        FinancePermission permission,
        CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        var member = await _companyMemberRepository.GetByCompanyAndUserAsync(CompanyId.From(companyId), actorUserId, ct);
        if (member is null)
            return false;

        return permission switch
        {
            FinancePermission.Read => member.Role is CompanyMembershipRole.Owner
                or CompanyMembershipRole.Manager,
            FinancePermission.ManagePayoutProfile => member.Role is CompanyMembershipRole.Owner
                or CompanyMembershipRole.Manager,
            _ => false
        };
    }
}
