using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Enums;

namespace Marketplace.Domain.Reviews.Entities;

public sealed class ProductReview : AuditableSoftDeleteAggregateRoot<ProductReviewId>
{
    private ProductReview() { }

    public ProductId ProductId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string? UserAvatar { get; private set; }
    public byte Rating { get; private set; }
    public string? Title { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public JsonBlob Images { get; private set; } = JsonBlob.Empty;
    public JsonBlob Pros { get; private set; } = JsonBlob.Empty;
    public JsonBlob Cons { get; private set; } = JsonBlob.Empty;
    public bool IsVerifiedPurchase { get; private set; }
    public OrderId? OrderId { get; private set; }
    public JsonBlob Helpful { get; private set; } = JsonBlob.Empty;
    public JsonBlob Replies { get; private set; } = JsonBlob.Empty;
    public ReviewModerationStatus Status { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }
    public DateTime? ModeratedAt { get; private set; }

    public static ProductReview Reconstitute(
        ProductReviewId id,
        ProductId productId,
        Guid userId,
        string userName,
        string? userAvatar,
        byte rating,
        string? title,
        string comment,
        JsonBlob images,
        JsonBlob pros,
        JsonBlob cons,
        bool isVerifiedPurchase,
        OrderId? orderId,
        JsonBlob helpful,
        JsonBlob replies,
        ReviewModerationStatus status,
        Guid? moderatedByUserId,
        DateTime? moderatedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ProductId = productId,
            UserId = userId,
            UserName = userName,
            UserAvatar = userAvatar,
            Rating = rating,
            Title = title,
            Comment = comment,
            Images = images,
            Pros = pros,
            Cons = cons,
            IsVerifiedPurchase = isVerifiedPurchase,
            OrderId = orderId,
            Helpful = helpful,
            Replies = replies,
            Status = status,
            ModeratedByUserId = moderatedByUserId,
            ModeratedAt = moderatedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
