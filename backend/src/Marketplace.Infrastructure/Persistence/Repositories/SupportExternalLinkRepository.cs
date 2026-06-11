using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SupportExternalLinkRepository : ISupportExternalLinkRepository
{
    private readonly ApplicationDbContext _context;

    public SupportExternalLinkRepository(ApplicationDbContext context) => _context = context;

    public async Task<SupportExternalLink?> GetByTicketAsync(SupportTicketId ticketId, string provider, CancellationToken ct = default)
    {
        var row = await _context.SupportExternalLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TicketId == ticketId.Value && x.Provider == provider, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<SupportExternalLink?> GetByExternalIdAsync(string provider, string externalTicketId, CancellationToken ct = default)
    {
        var row = await _context.SupportExternalLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalTicketId == externalTicketId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<SupportExternalLink> UpsertAsync(SupportExternalLink link, CancellationToken ct = default)
    {
        var row = await _context.SupportExternalLinks
            .FirstOrDefaultAsync(x => x.TicketId == link.TicketId.Value && x.Provider == link.Provider, ct);

        if (row is null)
        {
            row = new SupportExternalLinkRecord
            {
                TicketId = link.TicketId.Value,
                Provider = link.Provider,
                ExternalTicketId = link.ExternalTicketId,
                SyncState = (short)link.SyncState,
                LastSyncedAt = link.LastSyncedAt,
                ExternalUpdatedAt = link.ExternalUpdatedAt,
                ExternalSequence = link.ExternalSequence,
                CreatedAt = link.CreatedAt,
                UpdatedAt = link.UpdatedAt
            };
            await _context.SupportExternalLinks.AddAsync(row, ct);
        }
        else
        {
            row.ExternalTicketId = link.ExternalTicketId;
            row.SyncState = (short)link.SyncState;
            row.LastSyncedAt = link.LastSyncedAt;
            row.ExternalUpdatedAt = link.ExternalUpdatedAt;
            row.ExternalSequence = link.ExternalSequence;
            row.UpdatedAt = link.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task<IReadOnlyList<SupportExternalLink>> ListOutOfSyncAsync(int limit, CancellationToken ct = default)
    {
        var rows = await _context.SupportExternalLinks.AsNoTracking()
            .Where(x => x.SyncState != (short)SupportExternalSyncState.Synced)
            .OrderBy(x => x.UpdatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    private static SupportExternalLink ToDomain(SupportExternalLinkRecord row) =>
        SupportExternalLink.Reconstitute(
            row.Id,
            SupportTicketId.From(row.TicketId),
            row.Provider,
            row.ExternalTicketId,
            (SupportExternalSyncState)row.SyncState,
            row.LastSyncedAt,
            row.ExternalUpdatedAt,
            row.ExternalSequence,
            row.CreatedAt,
            row.UpdatedAt);
}
