using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marketplace.Infrastructure.Health;

public sealed class OutboxQueueHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public OutboxQueueHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var pending = await _db.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.DeadLetteredAtUtc == null && x.ProcessedAtUtc == null, ct);

            var oldest = await _db.OutboxMessages.AsNoTracking()
                .Where(x => x.DeadLetteredAtUtc == null && x.ProcessedAtUtc == null)
                .OrderBy(x => x.NextAttemptAtUtc ?? x.OccurredAtUtc)
                .Select(x => (DateTime?)(x.NextAttemptAtUtc ?? x.OccurredAtUtc))
                .FirstOrDefaultAsync(ct);

            var ageMinutes = oldest.HasValue ? (now - oldest.Value).TotalMinutes : 0;
            if (pending > 500 || ageMinutes > 30)
                return HealthCheckResult.Degraded($"Outbox backlog pending={pending}, oldestAgeMin={ageMinutes:F0}");

            return HealthCheckResult.Healthy($"Outbox pending={pending}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Outbox queue check failed", ex);
        }
    }
}
