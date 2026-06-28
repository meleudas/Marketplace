using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanyReview : AuditableSoftDeleteAggregateRoot<CompanyReviewId>
{
    private CompanyReview() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public long? OrderId { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public JsonBlob Ratings { get; private set; } = JsonBlob.Empty;
    public decimal OverallRating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public CompanyReviewStatus Status { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }
    public DateTime? ModeratedAt { get; private set; }

    public static CompanyReview Reconstitute(
        CompanyReviewId id,
        CompanyId companyId,
        Guid userId,
        string userName,
        long? orderId,
        bool isVerifiedPurchase,
        JsonBlob ratings,
        decimal overallRating,
        string comment,
        CompanyReviewStatus status,
        Guid? moderatedByUserId,
        DateTime? moderatedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            UserId = userId,
            UserName = userName,
            OrderId = orderId,
            IsVerifiedPurchase = isVerifiedPurchase,
            Ratings = ratings,
            OverallRating = overallRating,
            Comment = comment,
            Status = status,
            ModeratedByUserId = moderatedByUserId,
            ModeratedAt = moderatedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public static CompanyReview Create(
        CompanyReviewId id,
        CompanyId companyId,
        Guid userId,
        string userName,
        long orderId,
        decimal overallRating,
        string comment,
        JsonBlob? ratings = null)
    {
        if (overallRating is < 1 or > 5)
            throw new DomainException("Overall rating must be in range [1..5]");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Comment is required");
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("User name is required");

        var now = DateTime.UtcNow;
        return new CompanyReview
        {
            Id = id,
            CompanyId = companyId,
            UserId = userId,
            UserName = userName.Trim(),
            OrderId = orderId,
            IsVerifiedPurchase = true,
            Ratings = ratings ?? JsonBlob.Empty,
            OverallRating = overallRating,
            Comment = comment.Trim(),
            Status = CompanyReviewStatus.Approved,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public void Update(decimal overallRating, string comment, JsonBlob? ratings = null)
    {
        if (overallRating is < 1 or > 5)
            throw new DomainException("Overall rating must be in range [1..5]");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Comment is required");

        OverallRating = overallRating;
        Comment = comment.Trim();
        Ratings = ratings ?? JsonBlob.Empty;
        Touch();
    }

    public void Moderate(CompanyReviewStatus status, Guid moderatedByUserId)
    {
        Status = status;
        ModeratedByUserId = moderatedByUserId;
        ModeratedAt = DateTime.UtcNow;
        Touch();
    }
}
