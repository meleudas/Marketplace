namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReturnRequestRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public short Status { get; set; }
    public short ReasonCode { get; set; }
    public string? Comment { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public long? RefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
