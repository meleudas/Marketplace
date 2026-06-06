namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReportEscalationRecord
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string EscalatedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
