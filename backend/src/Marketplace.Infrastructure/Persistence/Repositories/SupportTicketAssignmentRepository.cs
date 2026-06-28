using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketAssignmentRepository : ISupportTicketAssignmentRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketAssignmentRepository(ApplicationDbContext context) => _context = context;

    public async Task<SupportTicketAssignment> AppendAsync(SupportTicketAssignment assignment, CancellationToken ct = default)
    {
        var row = new SupportTicketAssignmentRecord
        {
            TicketId = assignment.TicketId.Value,
            AssigneeUserId = assignment.AssigneeUserId,
            AssignedByUserId = assignment.AssignedByUserId,
            Reason = assignment.Reason,
            CreatedAt = assignment.CreatedAt
        };
        await _context.SupportTicketAssignments.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return SupportTicketAssignment.Reconstitute(
            row.Id,
            SupportTicketId.From(row.TicketId),
            row.AssigneeUserId,
            row.AssignedByUserId,
            row.Reason,
            row.CreatedAt);
    }

    public async Task<IReadOnlyList<SupportTicketAssignment>> ListByTicketAsync(SupportTicketId ticketId, int limit, CancellationToken ct = default)
    {
        var rows = await _context.SupportTicketAssignments.AsNoTracking()
            .Where(x => x.TicketId == ticketId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);
        return rows.Select(row => SupportTicketAssignment.Reconstitute(
            row.Id,
            SupportTicketId.From(row.TicketId),
            row.AssigneeUserId,
            row.AssignedByUserId,
            row.Reason,
            row.CreatedAt)).ToList();
    }
}
