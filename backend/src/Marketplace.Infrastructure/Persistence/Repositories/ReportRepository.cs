using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Entities;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetByIdAsync(ReportId id, CancellationToken ct = default)
    {
        var row = await _context.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Report>> ListByReporterAsync(string reporterUserId, CancellationToken ct = default)
    {
        var rows = await _context.Reports.AsNoTracking()
            .Where(x => x.ReporterUserId == reporterUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Report>> ListModerationQueueAsync(int limit, CancellationToken ct = default)
    {
        var rows = await _context.Reports.AsNoTracking()
            .Where(x => x.Status == (short)ReportStatus.New
                || x.Status == (short)ReportStatus.InReview
                || x.Status == (short)ReportStatus.Escalated)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .Take(Math.Max(1, limit))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Report>> ListRecentDuplicatesAsync(string reporterUserId, ReportTargetType targetType, string targetId, ReportReason reason, DateTime sinceUtc, CancellationToken ct = default)
    {
        var rows = await _context.Reports.AsNoTracking()
            .Where(x => x.ReporterUserId == reporterUserId
                && x.TargetType == (short)targetType
                && x.TargetId == targetId
                && x.Reason == (short)reason
                && x.CreatedAt >= sinceUtc)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Report> AddAsync(Report entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.Reports.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Report entity, CancellationToken ct = default)
    {
        var row = await _context.Reports.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Report '{entity.Id.Value}' was not found.");

        row.Status = (short)entity.Status;
        row.AssignedModeratorId = entity.AssignedModeratorId;
        row.AssignedAt = entity.AssignedAt;
        row.ReviewedById = entity.ReviewedById;
        row.ReviewedAt = entity.ReviewedAt;
        row.Resolution = entity.Resolution;
        row.ClosedById = entity.ClosedById;
        row.ClosedAt = entity.ClosedAt;
        row.LastActionReason = entity.LastActionReason;
        row.UpdatedAt = entity.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static Report ToDomain(ReportRecord row) =>
        Report.Reconstitute(
            ReportId.From(row.Id),
            row.ReporterUserId,
            (ReportTargetType)row.TargetType,
            row.TargetId,
            (ReportReason)row.Reason,
            row.Description,
            new JsonBlob(row.Images),
            (ReportStatus)row.Status,
            row.ReviewedById,
            row.ReviewedAt,
            row.Resolution,
            row.AssignedModeratorId,
            row.AssignedAt,
            row.ClosedById,
            row.ClosedAt,
            row.LastActionReason,
            (ReportPriority)row.Priority,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ReportRecord ToRecord(Report entity) =>
        new()
        {
            Id = entity.Id.Value,
            ReporterUserId = entity.ReporterUserId,
            TargetType = (short)entity.TargetType,
            TargetId = entity.TargetId,
            Reason = (short)entity.Reason,
            Description = entity.Description,
            Images = entity.Images.Raw ?? "[]",
            Status = (short)entity.Status,
            ReviewedById = entity.ReviewedById,
            ReviewedAt = entity.ReviewedAt,
            Resolution = entity.Resolution,
            Priority = (short)entity.Priority,
            AssignedModeratorId = entity.AssignedModeratorId,
            AssignedAt = entity.AssignedAt,
            ClosedById = entity.ClosedById,
            ClosedAt = entity.ClosedAt,
            LastActionReason = entity.LastActionReason,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
