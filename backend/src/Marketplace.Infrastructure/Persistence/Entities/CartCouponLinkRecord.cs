namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CartCouponLinkRecord
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long CouponId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string ValidationSnapshotRaw { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
