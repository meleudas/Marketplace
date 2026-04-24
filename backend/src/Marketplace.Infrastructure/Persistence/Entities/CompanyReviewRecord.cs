namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CompanyReviewRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public string RatingsRaw { get; set; } = "{}";
    public decimal OverallRating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public short Status { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
