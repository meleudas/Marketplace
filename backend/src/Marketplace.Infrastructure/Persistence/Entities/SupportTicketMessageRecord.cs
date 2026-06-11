namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SupportTicketMessageRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Attachments { get; set; }
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
