using Marketplace.Domain.Common.Exceptions;
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
            Status = status,
            ModeratedByUserId = moderatedByUserId,
            ModeratedAt = moderatedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public static ProductReview Create(
        ProductReviewId id,
        ProductId productId,
        Guid userId,
        string userName,
        string? userAvatar,
        byte rating,
        string? title,
        string comment,
        bool isVerifiedPurchase,
        OrderId? orderId,
        JsonBlob? images = null,
        JsonBlob? pros = null,
        JsonBlob? cons = null)
    {
        if (!isVerifiedPurchase)
            throw new DomainException("Verified purchase is required");
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be in range [1..5]");
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("User name is required");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Comment is required");

        var now = DateTime.UtcNow;
        return new ProductReview
        {
            Id = id,
            ProductId = productId,
            UserId = userId,
            UserName = userName.Trim(),
            UserAvatar = userAvatar,
            Rating = rating,
            Title = title?.Trim(),
            Comment = comment.Trim(),
            Images = images ?? JsonBlob.Empty,
            Pros = pros ?? JsonBlob.Empty,
            Cons = cons ?? JsonBlob.Empty,
            IsVerifiedPurchase = true,
            OrderId = orderId,
            Helpful = JsonBlob.Empty,
            Status = ReviewModerationStatus.Approved,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public void Update(string? title, string comment, byte rating, JsonBlob? pros = null, JsonBlob? cons = null)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be in range [1..5]");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Comment is required");

        Title = title?.Trim();
        Comment = comment.Trim();
        Rating = rating;
        Pros = pros ?? JsonBlob.Empty;
        Cons = cons ?? JsonBlob.Empty;
        Touch();
    }

    public void Moderate(ReviewModerationStatus status, Guid moderatedByUserId)
    {
        Status = status;
        ModeratedByUserId = moderatedByUserId;
        ModeratedAt = DateTime.UtcNow;
        Touch();
    }
}
