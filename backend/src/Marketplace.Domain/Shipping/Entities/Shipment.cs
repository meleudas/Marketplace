using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class Shipment : AuditableSoftDeleteAggregateRoot<ShipmentId>
{
    private Shipment() { }

    public OrderId OrderId { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public int ShipmentNumber { get; private set; }
    public ShippingMethodId ShippingMethodId { get; private set; } = null!;
    public WarehouseId? WarehouseId { get; private set; }
    public ShippingCarrierCode CarrierCode { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public JsonBlob RawPayload { get; private set; } = JsonBlob.Empty;

    public static Shipment Create(
        ShipmentId id,
        OrderId orderId,
        Guid customerId,
        int shipmentNumber,
        ShippingMethodId shippingMethodId,
        ShippingCarrierCode carrierCode,
        WarehouseId? warehouseId = null,
        string? trackingNumber = null)
    {
        var now = DateTime.UtcNow;
        return new Shipment
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            ShipmentNumber = shipmentNumber,
            ShippingMethodId = shippingMethodId,
            WarehouseId = warehouseId,
            CarrierCode = carrierCode,
            Status = DeliveryStatus.Created,
            TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim(),
            LastSyncedAtUtc = null,
            RawPayload = JsonBlob.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public void AssignTracking(string trackingNumber)
    {
        EnsureNotDeleted();
        TrackingNumber = trackingNumber.Trim();
        if (Status == DeliveryStatus.Created)
            Status = DeliveryStatus.LabelGenerated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDeliveryStatus(DeliveryStatus status)
    {
        EnsureNotDeleted();
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSynced(JsonBlob rawPayload)
    {
        EnsureNotDeleted();
        RawPayload = rawPayload;
        LastSyncedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Shipment Reconstitute(
        ShipmentId id,
        OrderId orderId,
        Guid customerId,
        int shipmentNumber,
        ShippingMethodId shippingMethodId,
        WarehouseId? warehouseId,
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
            ShipmentNumber = shipmentNumber,
            ShippingMethodId = shippingMethodId,
            WarehouseId = warehouseId,
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

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted shipment");
    }
}
