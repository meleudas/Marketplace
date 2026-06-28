namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ProductReviewRecord
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public byte Rating { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ImagesRaw { get; set; } = "{}";
    public string ProsRaw { get; set; } = "{}";
    public string ConsRaw { get; set; } = "{}";
    public bool IsVerifiedPurchase { get; set; }
    public long? OrderId { get; set; }
    public string HelpfulRaw { get; set; } = "{}";
    public short Status { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
