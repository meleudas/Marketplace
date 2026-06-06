using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class Shipment : AuditableSoftDeleteAggregateRoot<ShipmentId>
{
    private Shipment() { }

    public OrderId OrderId { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public ShippingMethodId ShippingMethodId { get; private set; } = null!;
    public ShippingCarrierCode CarrierCode { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public JsonBlob RawPayload { get; private set; } = JsonBlob.Empty;

    public static Shipment Reconstitute(
        ShipmentId id,
        OrderId orderId,
        Guid customerId,
        ShippingMethodId shippingMethodId,
        ShippingCarrierCode carrierCode,
        DeliveryStatus status,
        string? trackingNumber,
        DateTime? lastSyncedAtUtc,
        JsonBlob rawPayload,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            ShippingMethodId = shippingMethodId,
            CarrierCode = carrierCode,
            Status = status,
            TrackingNumber = trackingNumber,
            LastSyncedAtUtc = lastSyncedAtUtc,
            RawPayload = rawPayload,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
