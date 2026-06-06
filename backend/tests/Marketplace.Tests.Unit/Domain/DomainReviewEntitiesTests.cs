using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Reviews.Entities;

namespace Marketplace.Tests;

[Trait("Suite", "Reviews")]
public sealed class DomainReviewEntitiesTests
{
    [Fact]
    public void ProductReview_Create_Requires_VerifiedPurchase()
    {
        Assert.Throws<DomainException>(() =>
            ProductReview.Create(
                ProductReviewId.From(0),
                ProductId.From(100),
                Guid.NewGuid(),
                "user",
                null,
                5,
                "title",
                "comment",
                false,
                null));
    }

    [Fact]
    public void ReviewReply_UpdateBody_Sets_IsEdited()
    {
        var reply = ReviewReply.CreateForProductReview(
            ReviewReplyId.From(0),
            ProductReviewId.From(11),
            CompanyId.From(Guid.NewGuid()),
            Guid.NewGuid(),
            "old");

        reply.UpdateBody("new body");

        Assert.True(reply.IsEdited);
        Assert.Equal("new body", reply.Body);
    }

    [Fact]
    public void ProductReview_Create_Rejects_Invalid_Rating()
    {
        Assert.Throws<DomainException>(() =>
            ProductReview.Create(
                ProductReviewId.From(0),
                ProductId.From(100),
                Guid.NewGuid(),
                "user",
                null,
                0,
                "title",
                "comment",
                true,
                OrderId.From(1)));
    }

    [Fact]
    public void CompanyReview_Create_Rejects_Empty_Comment()
    {
        Assert.Throws<DomainException>(() =>
            CompanyReview.Create(
                CompanyReviewId.From(0),
                CompanyId.From(Guid.NewGuid()),
                Guid.NewGuid(),
                "user",
                1,
                5,
                "   "));
    }
}
