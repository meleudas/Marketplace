using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Reviews.Entities;

namespace Marketplace.Application.Reviews.Mappings;

public static class ReviewMapper
{
    public static ReviewDto ToDto(ProductReview review, ReviewReply? reply) =>
        new(
            review.Id.Value,
            "product",
            review.ProductId.Value,
            null,
            review.UserId,
            review.UserName,
            review.Rating,
            null,
            review.Title,
            review.Comment,
            review.IsVerifiedPurchase,
            (short)review.Status,
            review.CreatedAt,
            review.UpdatedAt,
            reply is null ? null : ToReplyDto(reply));

    public static ReviewDto ToDto(CompanyReview review, ReviewReply? reply) =>
        new(
            review.Id.Value,
            "company",
            null,
            review.CompanyId.Value,
            review.UserId,
            review.UserName,
            null,
            review.OverallRating,
            null,
            review.Comment,
            review.IsVerifiedPurchase,
            (short)review.Status,
            review.CreatedAt,
            review.UpdatedAt,
            reply is null ? null : ToReplyDto(reply));

    public static ReviewReplyDto ToReplyDto(ReviewReply reply) =>
        new(
            reply.Id.Value,
            reply.CompanyId.Value,
            reply.AuthorUserId,
            reply.Body,
            reply.IsEdited,
            reply.CreatedAt,
            reply.UpdatedAt);
}
