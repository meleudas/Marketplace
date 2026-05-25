using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxWriter
{
    private readonly ApplicationDbContext _context;

    public OutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
    {
        var row = new OutboxMessageRecord
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            Payload = payload,
            OccurredAtUtc = DateTime.UtcNow,
            Attempts = 0,
            NextAttemptAtUtc = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
    {
        var rows = await _context.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedAtUtc == null
                        && x.DeadLetteredAtUtc == null
                        && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= utcNow))
            .OrderBy(x => x.OccurredAtUtc)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(ct);

        return rows
            .Select(x => new OutboxMessage(
                x.Id,
                x.AggregateType,
                x.AggregateId,
                x.EventType,
                x.Payload,
                x.OccurredAtUtc,
                x.ProcessedAtUtc,
                x.Attempts,
                x.LastError,
                x.NextAttemptAtUtc,
                x.DeadLetteredAtUtc,
                x.DeadLetterReason,
                x.DeadLetterCategory))
            .ToList();
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.ProcessedAtUtc = DateTime.UtcNow;
        row.LastError = null;
        row.DeadLetteredAtUtc = null;
        row.DeadLetterReason = null;
        row.DeadLetterCategory = null;
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default)
    {
        var row = await _context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.Attempts += 1;
        row.LastError = error.Length > 2000 ? error[..2000] : error;
        row.NextAttemptAtUtc = nextAttemptAtUtc;
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default)
    {
        var row = await _context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.Attempts += 1;
        row.DeadLetteredAtUtc = DateTime.UtcNow;
        row.DeadLetterReason = reason.Length > 2000 ? reason[..2000] : reason;
        row.DeadLetterCategory = category.Length > 64 ? category[..64] : category;
        row.NextAttemptAtUtc = null;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return;

        row.DeadLetteredAtUtc = null;
        row.DeadLetterReason = null;
        row.DeadLetterCategory = null;
        row.LastError = null;
        row.NextAttemptAtUtc = DateTime.UtcNow;
        row.ProcessedAtUtc = null;
        await _context.SaveChangesAsync(ct);
    }
}
