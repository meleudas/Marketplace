using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class ShippingEvent : AuditableSoftDeleteAggregateRoot<ShippingEventId>
{
    private ShippingEvent() { }

    public ShippingCarrierCode CarrierCode { get; private set; }
    public string EventKey { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public JsonBlob RawPayload { get; private set; } = JsonBlob.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public ShipmentId? ShipmentId { get; private set; }
    public OrderId? OrderId { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DeliveryStatus? DeliveryStatus { get; private set; }
    public DateTime? OccurredAtUtc { get; private set; }

    public static ShippingEvent CreateFromWebhook(
        ShippingEventId id,
        ShippingCarrierCode carrierCode,
        string eventKey,
        string payloadHash,
        JsonBlob rawPayload,
        ShipmentId? shipmentId,
        OrderId? orderId,
        string? trackingNumber,
        DeliveryStatus? deliveryStatus,
        DateTime? occurredAtUtc)
    {
        var now = DateTime.UtcNow;
        return new ShippingEvent
        {
            Id = id,
            CarrierCode = carrierCode,
            EventKey = eventKey,
            PayloadHash = payloadHash,
            RawPayload = rawPayload,
            ReceivedAtUtc = now,
            ShipmentId = shipmentId,
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            DeliveryStatus = deliveryStatus,
            OccurredAtUtc = occurredAtUtc ?? now,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ShippingEvent Reconstitute(
        ShippingEventId id,
        ShippingCarrierCode carrierCode,
        string eventKey,
        string payloadHash,
        JsonBlob rawPayload,
        DateTime receivedAtUtc,
        ShipmentId? shipmentId,
        OrderId? orderId,
        string? trackingNumber,
        DeliveryStatus? deliveryStatus,
        DateTime? occurredAtUtc,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CarrierCode = carrierCode,
            EventKey = eventKey,
            PayloadHash = payloadHash,
            RawPayload = rawPayload,
            ReceivedAtUtc = receivedAtUtc,
            ShipmentId = shipmentId,
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            DeliveryStatus = deliveryStatus,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
