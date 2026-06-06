using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(ShipmentId id, CancellationToken ct = default);
    Task<Shipment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListByCustomerAsync(Guid userId, CancellationToken ct = default);
    Task<Shipment> AddAsync(Shipment entity, CancellationToken ct = default);
    Task UpdateAsync(Shipment entity, CancellationToken ct = default);
}
