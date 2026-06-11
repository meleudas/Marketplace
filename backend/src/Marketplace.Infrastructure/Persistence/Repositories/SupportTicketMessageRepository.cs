using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketMessageRepository : ISupportTicketMessageRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketMessageRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<SupportTicketMessage>> ListByTicketAsync(
        SupportTicketId ticketId,
        bool includeInternal,
        CancellationToken ct = default)
    {
        var query = _context.SupportTicketMessages.AsNoTracking()
            .Where(x => x.TicketId == ticketId.Value);
        if (!includeInternal)
            query = query.Where(x => !x.IsInternal);

        var rows = await query.OrderBy(x => x.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<SupportTicketMessage> AddAsync(SupportTicketMessage entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.SupportTicketMessages.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    private static SupportTicketMessage ToDomain(SupportTicketMessageRecord row) =>
        SupportTicketMessage.Reconstitute(
            SupportTicketMessageId.From(row.Id),
            SupportTicketId.From(row.TicketId),
            row.SenderId,
            row.Message,
            new JsonBlob(row.Attachments ?? "[]"),
            row.IsInternal,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static SupportTicketMessageRecord ToRecord(SupportTicketMessage entity) =>
        new()
        {
            Id = entity.Id.Value,
            TicketId = entity.TicketId.Value,
            SenderId = entity.SenderId,
            Message = entity.Message,
            Attachments = entity.Attachments.Raw ?? "[]",
            IsInternal = entity.IsInternal,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
