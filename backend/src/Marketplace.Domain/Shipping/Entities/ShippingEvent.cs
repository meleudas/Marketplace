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

    public static ShippingEvent Reconstitute(
        ShippingEventId id,
        ShippingCarrierCode carrierCode,
        string eventKey,
        string payloadHash,
        JsonBlob rawPayload,
        DateTime receivedAtUtc,
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
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
