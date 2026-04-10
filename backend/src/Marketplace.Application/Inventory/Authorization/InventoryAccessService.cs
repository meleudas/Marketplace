using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Application.Inventory.Authorization;

public interface IInventoryAccessService
{
    Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, InventoryPermission permission, CancellationToken ct = default);
}

public sealed class InventoryAccessService : IInventoryAccessService
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public InventoryAccessService(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, InventoryPermission permission, CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        var member = await _companyMemberRepository.GetByCompanyAndUserAsync(CompanyId.From(companyId), actorUserId, ct);
        if (member is null)
            return false;

        return permission switch
        {
            InventoryPermission.ReadInternal => true,
            InventoryPermission.WriteStock => member.Role is CompanyMembershipRole.Owner or CompanyMembershipRole.Manager or CompanyMembershipRole.Logistics,
            _ => false
        };
    }
}
