using Marketplace.Application.Common.Observability;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Jobs;

public sealed class BehaviorAggregationJobs
{
    private readonly ApplicationDbContext _db;

    public BehaviorAggregationJobs(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AggregateDailyAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var grouped = await _db.BehaviorEventRaw.AsNoTracking()
            .Where(x => x.OccurredAtUtc >= from && x.OccurredAtUtc <= to && !x.IsDeleted)
            .GroupBy(x => x.EventType)
            .Select(x => new { EventType = x.Key, Count = x.LongCount() })
            .ToListAsync(ct);

        foreach (var item in grouped)
        {
            var existing = await _db.BehaviorDailyAggregates.FirstOrDefaultAsync(x => x.Date == today && x.EventType == item.EventType, ct);
            if (existing is null)
            {
                _db.BehaviorDailyAggregates.Add(new Persistence.Entities.BehaviorDailyAggregateRecord
                {
                    Date = today,
                    EventType = item.EventType,
                    Count = item.Count,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Count = item.Count;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task PruneRawRetentionAsync(int days = 90, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var affected = await _db.BehaviorEventRaw
            .Where(x => x.OccurredAtUtc < threshold && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, DateTime.UtcNow)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
        if (affected == 0)
            return;
        MarketplaceMetrics.AnalyticsEventsDropped.Add(affected);
    }
}
