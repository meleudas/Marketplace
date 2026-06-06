using Marketplace.Domain.Reports.Entities;

namespace Marketplace.Application.Reports.Policies;

public static class ReportDedupPolicy
{
    public static bool HasRecentDuplicate(IEnumerable<Report> reports, DateTime nowUtc, TimeSpan window)
        => reports.Any(x => nowUtc - x.CreatedAt <= window);
}
