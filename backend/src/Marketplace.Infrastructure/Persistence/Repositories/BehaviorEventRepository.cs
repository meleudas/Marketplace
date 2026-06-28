using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Behavior.ValueObjects;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class BehaviorEventRepository : IBehaviorEventRepository
{
    private readonly ApplicationDbContext _context;

    public BehaviorEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BehaviorEvent> AddAsync(BehaviorEvent entity, CancellationToken ct = default)
    {
        var row = new BehaviorEventRawRecord
        {
            Id = entity.Id.Value,
            UserId = entity.UserId,
            SessionId = entity.SessionId,
            EventType = (short)entity.EventType,
            EventVersion = entity.EventVersion.Value,
            EventKey = entity.EventKey,
            Payload = entity.Payload.Raw ?? "{}",
            Source = entity.Source,
            OccurredAtUtc = entity.OccurredAtUtc,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
        await _context.BehaviorEventRaw.AddAsync(row, ct);
        await _context.BehaviorEventDedup.AddAsync(new BehaviorEventDedupRecord
        {
            EventKey = row.EventKey,
            EventType = row.EventType,
            BucketStartedAtUtc = new DateTime(row.OccurredAtUtc.Year, row.OccurredAtUtc.Month, row.OccurredAtUtc.Day, row.OccurredAtUtc.Hour, row.OccurredAtUtc.Minute, 0, DateTimeKind.Utc),
            CreatedAt = row.CreatedAt
        }, ct);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<IReadOnlyList<BehaviorEvent>> ListRecentDuplicatesAsync(string eventKey, BehaviorEventType eventType, DateTime sinceUtc, CancellationToken ct = default)
    {
        var rows = await _context.BehaviorEventRaw.AsNoTracking()
            .Where(x => x.EventKey == eventKey && x.EventType == (short)eventType && x.OccurredAtUtc >= sinceUtc)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public Task<int> CountByTypeAsync(BehaviorEventType eventType, DateTime sinceUtc, DateTime untilUtc, CancellationToken ct = default)
        => _context.BehaviorEventRaw.AsNoTracking()
            .Where(x => x.EventType == (short)eventType && x.OccurredAtUtc >= sinceUtc && x.OccurredAtUtc <= untilUtc)
            .CountAsync(ct);

    public async Task SoftDeleteByUserIdAsync(Guid userId, DateTime deletedAtUtc, CancellationToken ct = default)
    {
        await _context.BehaviorEventRaw
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, deletedAtUtc)
                .SetProperty(x => x.UpdatedAt, deletedAtUtc), ct);
    }

    private static BehaviorEvent ToDomain(BehaviorEventRawRecord row) =>
        BehaviorEvent.Reconstitute(
            AnalyticsEventId.From(row.Id),
            row.UserId,
            row.SessionId,
            (BehaviorEventType)row.EventType,
            BehaviorEventVersion.From(row.EventVersion),
            row.EventKey,
            new JsonBlob(row.Payload),
            row.Source,
            row.OccurredAtUtc,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
}
