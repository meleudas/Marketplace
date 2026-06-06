using Marketplace.Domain.Reports.Enums;

namespace Marketplace.Domain.Reports.Repositories;

public interface IReportActionAuditRepository
{
    Task AppendAsync(long reportId, ReportActionType actionType, string actorUserId, string reason, DateTime createdAtUtc, CancellationToken ct = default);
}
