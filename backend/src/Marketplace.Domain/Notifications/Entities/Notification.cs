using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Notifications.Enums;

namespace Marketplace.Domain.Notifications.Entities;

public sealed class Notification : AuditableSoftDeleteAggregateRoot<NotificationId>
{
    private Notification() { }

    public Guid UserId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public JsonBlob Data { get; private set; } = JsonBlob.Empty;
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? RawPayload { get; private set; }
    public Guid? CorrelationId { get; private set; }

    public static Notification Create(
        Guid userId,
        NotificationKind kind,
        string title,
        string message,
        JsonBlob data,
        string? actionUrl,
        Guid? correlationId,
        DateTime? expiresAt,
        string? rawPayload,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        return new Notification
        {
            Id = NotificationId.From(0),
            UserId = userId,
            Kind = kind,
            Title = title.Trim(),
            Message = message.Trim(),
            Data = data,
            ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim(),
            IsRead = false,
            ReadAt = null,
            ExpiresAt = expiresAt,
            RawPayload = rawPayload,
            CorrelationId = correlationId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public static Notification Reconstitute(
        NotificationId id,
        Guid userId,
        NotificationKind kind,
        string title,
        string message,
        JsonBlob data,
        string? actionUrl,
        bool isRead,
        DateTime? readAt,
        DateTime? expiresAt,
        string? rawPayload,
        Guid? correlationId,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Kind = kind,
            Title = title,
            Message = message,
            Data = data,
            ActionUrl = actionUrl,
            IsRead = isRead,
            ReadAt = readAt,
            ExpiresAt = expiresAt,
            RawPayload = rawPayload,
            CorrelationId = correlationId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void MarkAsRead(DateTime utcNow)
    {
        if (IsRead)
            return;
        IsRead = true;
        ReadAt = utcNow;
        Touch();
    }
}
