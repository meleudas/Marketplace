using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Entities;
using Marketplace.Domain.Reports.Enums;

namespace Marketplace.Domain.Reports.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(ReportId id, CancellationToken ct = default);
    Task<IReadOnlyList<Report>> ListByReporterAsync(string reporterUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Report>> ListModerationQueueAsync(int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Report>> ListRecentDuplicatesAsync(
        string reporterUserId,
        ReportTargetType targetType,
        string targetId,
        ReportReason reason,
        DateTime sinceUtc,
        CancellationToken ct = default);
    Task<Report> AddAsync(Report entity, CancellationToken ct = default);
    Task UpdateAsync(Report entity, CancellationToken ct = default);
}
