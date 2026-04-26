namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class OrderStatusHistoryRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public short OldStatus { get; set; }
    public short NewStatus { get; set; }
    public string? Comment { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime ChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
