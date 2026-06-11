using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketEventRepository : ISupportTicketEventRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketEventRepository(ApplicationDbContext context) => _context = context;

    public async Task<SupportTicketEvent> AppendAsync(SupportTicketEvent ticketEvent, CancellationToken ct = default)
    {
        var row = new SupportTicketEventRecord
        {
            TicketId = ticketEvent.TicketId.Value,
            EventType = (short)ticketEvent.EventType,
            ActorUserId = ticketEvent.ActorUserId,
            Reason = ticketEvent.Reason,
            Payload = ticketEvent.Payload.Raw ?? "{}",
            CreatedAt = ticketEvent.CreatedAt
        };
        await _context.SupportTicketEvents.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return SupportTicketEvent.Reconstitute(
            row.Id,
            SupportTicketId.From(row.TicketId),
            ticketEvent.EventType,
            row.ActorUserId,
            row.Reason,
            new JsonBlob(row.Payload),
            row.CreatedAt);
    }

    public async Task<IReadOnlyList<SupportTicketEvent>> ListByTicketAsync(SupportTicketId ticketId, int limit, CancellationToken ct = default)
    {
        var rows = await _context.SupportTicketEvents.AsNoTracking()
            .Where(x => x.TicketId == ticketId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);
        return rows.Select(row => SupportTicketEvent.Reconstitute(
            row.Id,
            SupportTicketId.From(row.TicketId),
            (Domain.Support.Enums.SupportTicketEventType)row.EventType,
            row.ActorUserId,
            row.Reason,
            new JsonBlob(row.Payload),
            row.CreatedAt)).ToList();
    }
}
