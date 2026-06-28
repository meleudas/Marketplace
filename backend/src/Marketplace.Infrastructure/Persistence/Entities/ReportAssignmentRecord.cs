namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReportAssignmentRecord
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string ModeratorUserId { get; set; } = string.Empty;
    public string AssignedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
