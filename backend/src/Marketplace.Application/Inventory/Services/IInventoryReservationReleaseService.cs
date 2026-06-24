using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Application.Inventory.Services;

public interface IInventoryReservationReleaseService
{
    Task ReleaseAsync(InventoryReservation reservation, Guid? actorUserId, string reason, bool expired, CancellationToken ct = default);
}
