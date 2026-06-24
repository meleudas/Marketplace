using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class ShipmentItem : AuditableSoftDeleteAggregateRoot<ShipmentItemId>
{
    private ShipmentItem() { }

    public ShipmentId ShipmentId { get; private set; } = null!;
    public OrderItemId OrderItemId { get; private set; } = null!;
    public int Quantity { get; private set; }

    public static ShipmentItem Create(ShipmentItemId id, ShipmentId shipmentId, OrderItemId orderItemId, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Shipment item quantity must be positive");

        var now = DateTime.UtcNow;
        return new ShipmentItem
        {
            Id = id,
            ShipmentId = shipmentId,
            OrderItemId = orderItemId,
            Quantity = quantity,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ShipmentItem Reconstitute(
        ShipmentItemId id,
        ShipmentId shipmentId,
        OrderItemId orderItemId,
        int quantity,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ShipmentId = shipmentId,
            OrderItemId = orderItemId,
            Quantity = quantity,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
