using Marketplace.Domain.Notifications.Enums;

namespace Marketplace.Application.Notifications.Ports;

public sealed record InAppNotificationListItemDto(
    long Id,
    string TemplateKey,
    Guid? CorrelationId,
    NotificationKind Kind,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAtUtc,
    string DataJson);

public sealed record PagedInAppNotificationsDto(
    IReadOnlyList<InAppNotificationListItemDto> Items,
    long Total,
    int Page,
    int PageSize);

/// <summary>Persists in-app notification rows. Insert is idempotent per (user_id, correlation_id) when correlation is set.</summary>
public interface IInAppNotificationRepository
{
    /// <returns>true if a new row was inserted; false if skipped due to idempotent duplicate.</returns>
    Task<bool> TryInsertAsync(
        Guid userId,
        NotificationKind kind,
        string title,
        string message,
        string dataJson,
        string? actionUrl,
        Guid? correlationId,
        DateTime? expiresAtUtc,
        string? rawPayload,
        CancellationToken ct = default);

    Task<PagedInAppNotificationsDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <returns>true if the row existed and belongs to the user (read state updated if needed).</returns>
    Task<bool> MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default);

    /// <summary>Видаляє прострочені in-app рядки (м’яке GDPR / обмеження обсягу).</summary>
    Task<int> DeleteExpiredBeforeAsync(DateTime utcNow, CancellationToken ct = default);
}
