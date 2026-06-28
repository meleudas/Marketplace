namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CouponUsageRecord
{
    public long Id { get; set; }
    public long CouponId { get; set; }
    public Guid UserId { get; set; }
    public long OrderId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public decimal DiscountAppliedAmount { get; set; }
    public DateTime ConsumedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
