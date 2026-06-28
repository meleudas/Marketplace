namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SupportTicketRecord
{
    public long Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public short Status { get; set; }
    public short Priority { get; set; }
    public long? CategoryId { get; set; }
    public string? AssignedToId { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
