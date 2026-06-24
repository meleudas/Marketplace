using Marketplace.Application.Common;
using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class IntegrationRetryRepository : IIntegrationRetryStore
{
    private readonly ApplicationDbContext _context;

    public IntegrationRetryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(IntegrationRetryUpsert request, DateTime nextAttemptAtUtc, CancellationToken ct = default)
    {
        var row = await _context.IntegrationRetries
            .FirstOrDefaultAsync(x =>
                x.Kind == request.Kind
                && x.AggregateType == request.AggregateType
                && x.AggregateId == request.AggregateId
                && x.ResolvedAtUtc == null
                && x.DeadLetteredAtUtc == null, ct);

        if (row is null)
        {
            row = new IntegrationRetryRecord
            {
                Id = Guid.NewGuid(),
                Kind = request.Kind,
                AggregateType = request.AggregateType,
                AggregateId = request.AggregateId,
                PayloadJson = request.PayloadJson,
                Attempts = 0,
                LastError = Truncate(request.Error),
                NextAttemptAtUtc = nextAttemptAtUtc,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _context.IntegrationRetries.AddAsync(row, ct);
        }
        else
        {
            row.PayloadJson = request.PayloadJson;
            row.LastError = Truncate(request.Error);
            row.NextAttemptAtUtc = nextAttemptAtUtc;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IntegrationRetryEntry>> ListDueAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
    {
        var rows = await _context.IntegrationRetries
            .AsNoTracking()
            .Where(x => x.ResolvedAtUtc == null
                        && x.DeadLetteredAtUtc == null
                        && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= utcNow))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(ct);

        return rows.Select(ToEntry).ToList();
    }

    public async Task MarkResolvedAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _context.IntegrationRetries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.ResolvedAtUtc = DateTime.UtcNow;
        row.LastError = null;
        row.NextAttemptAtUtc = null;
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default)
    {
        var row = await _context.IntegrationRetries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.Attempts += 1;
        row.LastError = Truncate(error);
        row.NextAttemptAtUtc = nextAttemptAtUtc;
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default)
    {
        var row = await _context.IntegrationRetries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.Attempts += 1;
        row.DeadLetteredAtUtc = DateTime.UtcNow;
        row.DeadLetterReason = Truncate(reason);
        row.DeadLetterCategory = category.Length > 64 ? category[..64] : category;
        row.NextAttemptAtUtc = null;
        await _context.SaveChangesAsync(ct);
    }

    private static IntegrationRetryEntry ToEntry(IntegrationRetryRecord x) =>
        new(
            x.Id,
            x.Kind,
            x.AggregateType,
            x.AggregateId,
            x.PayloadJson,
            x.Attempts,
            x.LastError,
            x.NextAttemptAtUtc,
            x.DeadLetteredAtUtc,
            x.DeadLetterCategory,
            x.CreatedAtUtc);

    private static string? Truncate(string? value)
        => string.IsNullOrEmpty(value) ? value : value.Length > 2000 ? value[..2000] : value;
}
