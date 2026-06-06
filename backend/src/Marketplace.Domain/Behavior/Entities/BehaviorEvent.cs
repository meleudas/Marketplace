using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.ValueObjects;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Entities;

public sealed class BehaviorEvent : AuditableSoftDeleteAggregateRoot<AnalyticsEventId>
{
    private BehaviorEvent() { }

    public Guid? UserId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public BehaviorEventType EventType { get; private set; }
    public BehaviorEventVersion EventVersion { get; private set; } = BehaviorEventVersion.V1;
    public string EventKey { get; private set; } = string.Empty;
    public JsonBlob Payload { get; private set; } = JsonBlob.Empty;
    public string Source { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    public static BehaviorEvent Create(
        Guid? userId,
        string sessionId,
        BehaviorEventType eventType,
        string eventKey,
        JsonBlob payload,
        string source,
        DateTime occurredAtUtc,
        DateTime nowUtc)
    {
        return new BehaviorEvent
        {
            Id = AnalyticsEventId.From(0),
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            EventVersion = BehaviorEventVersion.V1,
            EventKey = eventKey,
            Payload = payload,
            Source = source,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public static BehaviorEvent Reconstitute(
        AnalyticsEventId id,
        Guid? userId,
        string sessionId,
        BehaviorEventType eventType,
        BehaviorEventVersion eventVersion,
        string eventKey,
        JsonBlob payload,
        string source,
        DateTime occurredAtUtc,
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
            EventVersion = eventVersion,
            EventKey = eventKey,
            Payload = payload,
            Source = source,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
