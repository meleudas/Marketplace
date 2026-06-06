namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReportRecord
{
    public long Id { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public short TargetType { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public short Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Images { get; set; } = "[]";
    public short Status { get; set; }
    public string? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Resolution { get; set; }
    public short Priority { get; set; }
    public string? AssignedModeratorId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ClosedById { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LastActionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
