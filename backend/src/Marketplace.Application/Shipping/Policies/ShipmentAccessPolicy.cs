using Marketplace.Application.Orders.Authorization;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Shipping.Policies;

public enum ShipmentPermission
{
    Read,
    Manage
}

public interface IShipmentAccessPolicy
{
    Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, ShipmentPermission permission, CancellationToken ct = default);
}

public sealed class ShipmentAccessPolicy : IShipmentAccessPolicy
{
    private readonly IOrderAccessService _orderAccess;

    public ShipmentAccessPolicy(IOrderAccessService orderAccess) => _orderAccess = orderAccess;

    public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, ShipmentPermission permission, CancellationToken ct = default)
    {
        var orderPermission = permission switch
        {
            ShipmentPermission.Read => OrderPermission.Read,
            ShipmentPermission.Manage => OrderPermission.ManageStatus,
            _ => OrderPermission.Read
        };
        return _orderAccess.HasAccessAsync(order, actorUserId, isActorAdmin, orderPermission, ct);
    }
}
