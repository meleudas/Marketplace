using Marketplace.Domain.Reports.Enums;

namespace Marketplace.Application.Reports.Policies;

public static class ReportSlaPolicy
{
    public static TimeSpan GetFirstReviewDeadline(ReportPriority priority) =>
        priority == ReportPriority.High ? TimeSpan.FromHours(2) : TimeSpan.FromHours(24);

    public static bool IsBreached(ReportPriority priority, DateTime createdAtUtc, DateTime nowUtc, ReportStatus status)
    {
        if (status is not (ReportStatus.New or ReportStatus.InReview))
            return false;
        return nowUtc - createdAtUtc > GetFirstReviewDeadline(priority);
    }
}
