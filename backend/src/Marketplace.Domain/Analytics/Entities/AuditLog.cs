using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Analytics.Entities;

public sealed class AuditLog : AuditableSoftDeleteAggregateRoot<AuditLogId>
{
    private AuditLog() { }

    public string? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public JsonBlob OldData { get; private set; } = JsonBlob.Empty;
    public JsonBlob NewData { get; private set; } = JsonBlob.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;

    public static AuditLog Reconstitute(
        AuditLogId id,
        string? userId,
        string action,
        string entityType,
        string entityId,
        JsonBlob oldData,
        JsonBlob newData,
        string ipAddress,
        string userAgent,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldData = oldData,
            NewData = newData,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
