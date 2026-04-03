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
    public JsonBlob? Response { get; private set; }
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
        JsonBlob? response,
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
            Response = response,
            Status = status,
            ModeratedByUserId = moderatedByUserId,
            ModeratedAt = moderatedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
