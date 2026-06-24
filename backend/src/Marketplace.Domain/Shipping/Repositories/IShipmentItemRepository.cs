using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShipmentItemRepository
{
    Task<IReadOnlyList<ShipmentItem>> ListByShipmentIdAsync(ShipmentId shipmentId, CancellationToken ct = default);
    Task<IReadOnlyList<ShipmentItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<ShipmentItem> items, CancellationToken ct = default);
}
