namespace Marketplace.Domain.Reports.Enums;

public enum ReportStatus : short
{
    New = 0,
    InReview = 1,
    Actioned = 2,
    Rejected = 3,
    Escalated = 4,
    Closed = 5
}
