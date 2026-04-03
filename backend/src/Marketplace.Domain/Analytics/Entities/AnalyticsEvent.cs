using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Analytics.Entities;

public sealed class AnalyticsEvent : AuditableSoftDeleteAggregateRoot<AnalyticsEventId>
{
    private AnalyticsEvent() { }

    public Guid? UserId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public JsonBlob EventData { get; private set; } = JsonBlob.Empty;
    public string DeviceType { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public string Os { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string? ContextRaw { get; private set; }

    public static AnalyticsEvent Reconstitute(
        AnalyticsEventId id,
        Guid? userId,
        string sessionId,
        string eventType,
        JsonBlob eventData,
        string deviceType,
        string browser,
        string os,
        string ipAddress,
        string? contextRaw,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            EventData = eventData,
            DeviceType = deviceType,
            Browser = browser,
            Os = os,
            IpAddress = ipAddress,
            ContextRaw = contextRaw,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
