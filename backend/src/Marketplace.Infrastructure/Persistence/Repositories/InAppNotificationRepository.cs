using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Notifications.Enums;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class InAppNotificationRepository : IInAppNotificationRepository
{
    private readonly ApplicationDbContext _db;

    public InAppNotificationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryInsertAsync(
        Guid userId,
        NotificationKind kind,
        string title,
        string message,
        string dataJson,
        string? actionUrl,
        Guid? correlationId,
        DateTime? expiresAtUtc,
        string? rawPayload,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var row = new NotificationRecord
        {
            UserId = userId,
            Type = (short)kind,
            Title = title,
            Message = message,
            Data = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson,
            ActionUrl = actionUrl,
            IsRead = false,
            ReadAt = null,
            ExpiresAt = expiresAtUtc,
            RawPayload = rawPayload,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };

        _db.Notifications.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (
            (ex.InnerException is PostgresException pg && pg.SqlState == "23505") ||
            (ex.InnerException?.Message?.Contains("UNIQUE constraint failed: notifications.UserId, notifications.CorrelationId", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            _db.Entry(row).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<PagedInAppNotificationsDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Notifications.AsNoTracking().Where(x => x.UserId == userId && !x.IsDeleted);
        var total = await q.LongCountAsync(ct);
        var rows = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var templateKey = TryReadTemplateKey(r.Data);
            return new InAppNotificationListItemDto(
                r.Id,
                templateKey,
                r.CorrelationId,
                (NotificationKind)r.Type,
                r.Title,
                r.Message,
                r.ActionUrl,
                r.IsRead,
                r.ReadAt,
                r.CreatedAt,
                r.Data);
        }).ToList();

        return new PagedInAppNotificationsDto(items, total, page, pageSize);
    }

    public async Task<bool> MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default)
    {
        var row = await _db.Notifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted,
            ct);
        if (row is null)
            return false;

        var now = DateTime.UtcNow;
        if (!row.IsRead)
        {
            row.IsRead = true;
            row.ReadAt = now;
            row.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<int> DeleteExpiredBeforeAsync(DateTime utcNow, CancellationToken ct = default) =>
        await _db.Notifications
            .Where(x => x.ExpiresAt != null && x.ExpiresAt < utcNow)
            .ExecuteDeleteAsync(ct);

    private static string TryReadTemplateKey(string dataJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
            if (doc.RootElement.TryGetProperty("templateKey", out var p))
                return p.GetString() ?? string.Empty;
        }
        catch
        {
            // ignored
        }

        return string.Empty;
    }
}
