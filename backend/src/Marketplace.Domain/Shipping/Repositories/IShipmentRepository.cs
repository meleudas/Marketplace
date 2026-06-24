using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(ShipmentId id, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListByCustomerAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListByStatusAsync(DeliveryStatus status, int limit, CancellationToken ct = default);
    Task<Shipment> AddAsync(Shipment entity, CancellationToken ct = default);
    Task UpdateAsync(Shipment entity, CancellationToken ct = default);
}
