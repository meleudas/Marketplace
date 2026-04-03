using Marketplace.Domain.Analytics.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Analytics.Entities;

public sealed class ActivityLog : AuditableSoftDeleteAggregateRoot<ActivityLogId>
{
    private ActivityLog() { }

    public string? UserId { get; private set; }
    public ActivityEventType EventType { get; private set; }
    public JsonBlob EventData { get; private set; } = JsonBlob.Empty;
    public string IpAddress { get; private set; } = string.Empty;

    public static ActivityLog Reconstitute(
        ActivityLogId id,
        string? userId,
        ActivityEventType eventType,
        JsonBlob eventData,
        string ipAddress,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            EventType = eventType,
            EventData = eventData,
            IpAddress = ipAddress,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
