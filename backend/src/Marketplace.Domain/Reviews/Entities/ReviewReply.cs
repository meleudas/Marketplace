using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Reviews.Entities;

public sealed class ReviewReply : AuditableSoftDeleteAggregateRoot<ReviewReplyId>
{
    private ReviewReply() { }

    public ProductReviewId? ProductReviewId { get; private set; }
    public CompanyReviewId? CompanyReviewId { get; private set; }
    public CompanyId CompanyId { get; private set; } = null!;
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsEdited { get; private set; }

    public static ReviewReply CreateForProductReview(
        ReviewReplyId id,
        ProductReviewId reviewId,
        CompanyId companyId,
        Guid authorUserId,
        string body)
    {
        ValidateBody(body);
        var now = DateTime.UtcNow;
        return new ReviewReply
        {
            Id = id,
            ProductReviewId = reviewId,
            CompanyReviewId = null,
            CompanyId = companyId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            IsEdited = false,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ReviewReply CreateForCompanyReview(
        ReviewReplyId id,
        CompanyReviewId reviewId,
        CompanyId companyId,
        Guid authorUserId,
        string body)
    {
        ValidateBody(body);
        var now = DateTime.UtcNow;
        return new ReviewReply
        {
            Id = id,
            ProductReviewId = null,
            CompanyReviewId = reviewId,
            CompanyId = companyId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            IsEdited = false,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ReviewReply Reconstitute(
        ReviewReplyId id,
        ProductReviewId? productReviewId,
        CompanyReviewId? companyReviewId,
        CompanyId companyId,
        Guid authorUserId,
        string body,
        bool isEdited,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ProductReviewId = productReviewId,
            CompanyReviewId = companyReviewId,
            CompanyId = companyId,
            AuthorUserId = authorUserId,
            Body = body,
            IsEdited = isEdited,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void UpdateBody(string body)
    {
        ValidateBody(body);
        Body = body.Trim();
        IsEdited = true;
        Touch();
    }

    private static void ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Reply body is required");
    }
}
