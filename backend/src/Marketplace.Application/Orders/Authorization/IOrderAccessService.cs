using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Orders.Authorization;

public interface IOrderAccessService
{
    Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default);
    Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default);
}
