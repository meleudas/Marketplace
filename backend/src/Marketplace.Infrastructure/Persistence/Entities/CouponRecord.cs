namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CouponRecord
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public short DiscountType { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public int UserUsageLimit { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public string? ApplicableCategoriesRaw { get; set; }
    public string? ApplicableProductsRaw { get; set; }
    public string? ApplicableCompaniesRaw { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
