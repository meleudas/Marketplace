namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReportActionRecord
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public short ActionType { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
