using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Orders.Authorization;

public sealed class OrderAccessService : IOrderAccessService
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public OrderAccessService(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        if (order.CustomerId == actorUserId)
            return permission is OrderPermission.Read or OrderPermission.Cancel;

        var member = await _companyMemberRepository.GetByCompanyAndUserAsync(order.CompanyId, actorUserId, ct);
        if (member is null || member.IsDeleted)
            return false;

        return permission switch
        {
            OrderPermission.Read => true,
            OrderPermission.ManageStatus => true,
            OrderPermission.Cancel => true,
            _ => false
        };
    }

    public async Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        var member = await _companyMemberRepository.GetByCompanyAndUserAsync(CompanyId.From(companyId), actorUserId, ct);
        return member is not null && !member.IsDeleted;
    }
}
