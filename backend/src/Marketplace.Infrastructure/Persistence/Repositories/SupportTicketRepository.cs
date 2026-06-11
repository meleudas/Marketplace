using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketRepository(ApplicationDbContext context) => _context = context;

    public async Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default)
    {
        var row = await _context.SupportTickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<SupportTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default)
    {
        var row = await _context.SupportTickets.AsNoTracking().FirstOrDefaultAsync(x => x.TicketNumber == ticketNumber, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<SupportTicket>> ListByUserAsync(string userId, int skip, int take, CancellationToken ct = default)
    {
        var rows = await _context.SupportTickets.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public Task<int> CountByUserAsync(string userId, CancellationToken ct = default) =>
        _context.SupportTickets.AsNoTracking().CountAsync(x => x.UserId == userId, ct);

    public Task<int> CountRecentByUserAsync(string userId, DateTime sinceUtc, CancellationToken ct = default) =>
        _context.SupportTickets.AsNoTracking().CountAsync(x => x.UserId == userId && x.CreatedAt >= sinceUtc, ct);

    public async Task<IReadOnlyList<SupportTicket>> ListPendingSyncAsync(int limit, CancellationToken ct = default)
    {
        var ticketIds = _context.SupportExternalLinks.AsNoTracking()
            .Where(x => x.SyncState != (short)SupportExternalSyncState.Synced)
            .Select(x => x.TicketId);

        var rows = await _context.SupportTickets.AsNoTracking()
            .Where(x => ticketIds.Contains(x.Id))
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<SupportTicket> AddAsync(SupportTicket entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.SupportTickets.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(SupportTicket entity, CancellationToken ct = default)
    {
        var row = await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Support ticket '{entity.Id.Value}' was not found.");

        row.Status = (short)entity.Status;
        row.Message = entity.Message;
        row.AssignedToId = entity.AssignedToId;
        row.LastMessageAt = entity.LastMessageAt;
        row.ResolvedAt = entity.ResolvedAt;
        row.ClosedAt = entity.ClosedAt;
        row.EscalatedAt = entity.EscalatedAt;
        row.SlaDueAt = entity.SlaDueAt;
        row.UpdatedAt = entity.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    internal static SupportTicket ToDomain(SupportTicketRecord row) =>
        SupportTicket.Reconstitute(
            SupportTicketId.From(row.Id),
            row.TicketNumber,
            row.UserId,
            row.OrderId.HasValue ? OrderId.From(row.OrderId.Value) : null,
            row.CompanyId.HasValue ? CompanyId.From(row.CompanyId.Value) : null,
            row.Subject,
            row.Message,
            (SupportTicketStatus)row.Status,
            (SupportTicketPriority)row.Priority,
            row.CategoryId.HasValue ? CategoryId.From(row.CategoryId.Value) : null,
            row.AssignedToId,
            row.LastMessageAt,
            row.ResolvedAt,
            row.ClosedAt,
            row.EscalatedAt,
            row.SlaDueAt,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    internal static SupportTicketRecord ToRecord(SupportTicket entity) =>
        new()
        {
            Id = entity.Id.Value,
            TicketNumber = entity.TicketNumber,
            UserId = entity.UserId,
            OrderId = entity.OrderId?.Value,
            CompanyId = entity.CompanyId?.Value,
            Subject = entity.Subject,
            Message = entity.Message,
            Status = (short)entity.Status,
            Priority = (short)entity.Priority,
            CategoryId = entity.CategoryId?.Value,
            AssignedToId = entity.AssignedToId,
            LastMessageAt = entity.LastMessageAt,
            ResolvedAt = entity.ResolvedAt,
            ClosedAt = entity.ClosedAt,
            EscalatedAt = entity.EscalatedAt,
            SlaDueAt = entity.SlaDueAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
