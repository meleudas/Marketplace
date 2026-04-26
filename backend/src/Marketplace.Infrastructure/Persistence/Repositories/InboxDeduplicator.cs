using Marketplace.Application.Common.Ports;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class InboxDeduplicator : IInboxDeduplicator
{
    private readonly ApplicationDbContext _context;

    public InboxDeduplicator(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
    {
        return _context.InboxMessages.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer, ct);
    }

    public async Task MarkProcessedAsync(Guid messageId, string consumer, string? metadata, CancellationToken ct = default)
    {
        var exists = await HasProcessedAsync(messageId, consumer, ct);
        if (exists)
            return;

        _context.InboxMessages.Add(new()
        {
            MessageId = messageId,
            Consumer = consumer,
            Metadata = metadata,
            ProcessedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
    }
}
