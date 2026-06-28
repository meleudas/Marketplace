using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ReportActionAuditRepository : IReportActionAuditRepository
{
    private readonly ApplicationDbContext _context;

    public ReportActionAuditRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AppendAsync(long reportId, ReportActionType actionType, string actorUserId, string reason, DateTime createdAtUtc, CancellationToken ct = default)
    {
        await _context.ReportActions.AddAsync(
            new ReportActionRecord
            {
                ReportId = reportId,
                ActionType = (short)actionType,
                ActorUserId = actorUserId,
                Reason = reason,
                CreatedAt = createdAtUtc
            },
            ct);

        if (actionType == ReportActionType.Assigned)
        {
            await _context.ReportAssignments.AddAsync(
                new ReportAssignmentRecord
                {
                    ReportId = reportId,
                    ModeratorUserId = actorUserId,
                    AssignedByUserId = actorUserId,
                    Reason = reason,
                    CreatedAt = createdAtUtc
                },
                ct);
        }

        if (actionType == ReportActionType.Escalated)
        {
            await _context.ReportEscalations.AddAsync(
                new ReportEscalationRecord
                {
                    ReportId = reportId,
                    EscalatedByUserId = actorUserId,
                    Reason = reason,
                    CreatedAt = createdAtUtc
                },
                ct);
        }

        await _context.SaveChangesAsync(ct);
    }
}
