using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests;

[Trait("Suite", "ProductsModeration")]
public sealed class DomainProductModerationTests
{
    [Fact]
    public void SubmitForModeration_FromDraft_SetsPendingReviewAndAuthor()
    {
        var author = Guid.NewGuid();
        var p = Product.Create(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "N",
            "slug",
            "d",
            new Money(10),
            null,
            0,
            0,
            CategoryId.From(1),
            false);
        Assert.Equal(ProductStatus.Draft, p.Status);

        p.SubmitForModeration(author);

        Assert.Equal(ProductStatus.PendingReview, p.Status);
        Assert.Equal(author, p.SubmittedByUserId);
        Assert.Null(p.ModerationRejectionReason);
    }

    [Fact]
    public void Approve_FromPendingReview_SetsActive()
    {
        var p = PendingProduct();
        p.Approve();
        Assert.Equal(ProductStatus.Active, p.Status);
    }

    [Fact]
    public void Reject_FromPendingReview_SetsDraftAndReason()
    {
        var p = PendingProduct();
        p.Reject("  bad photos  ");
        Assert.Equal(ProductStatus.Draft, p.Status);
        Assert.Equal("bad photos", p.ModerationRejectionReason);
    }

    [Fact]
    public void Approve_FromActive_Throws()
    {
        var p = Product.Reconstitute(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "N",
            "s",
            "d",
            new Money(1),
            null,
            1,
            0,
            CategoryId.From(1),
            ProductStatus.Active,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
        Assert.Throws<DomainException>(() => p.Approve());
    }

    [Fact]
    public void UpdateProfile_WhenArchived_Throws()
    {
        var p = Product.Reconstitute(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "N",
            "s",
            "d",
            new Money(1),
            null,
            1,
            0,
            CategoryId.From(1),
            ProductStatus.Archived,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
        Assert.Throws<DomainException>(() =>
            p.UpdateProfile("X", "x", "d", new Money(2), null, 0, CategoryId.From(1), false));
    }

    [Fact]
    public void SubmitForModeration_With_Empty_Submitter_Throws()
    {
        var p = Product.Create(
            ProductId.From(9),
            CompanyId.From(Guid.NewGuid()),
            "N",
            "slug",
            "d",
            new Money(10),
            null,
            0,
            0,
            CategoryId.From(1),
            false);

        Assert.Throws<DomainException>(() => p.SubmitForModeration(Guid.Empty));
    }

    private static Product PendingProduct()
    {
        var p = Product.Create(
            ProductId.From(9),
            CompanyId.From(Guid.NewGuid()),
            "N",
            "slug",
            "d",
            new Money(10),
            null,
            0,
            0,
            CategoryId.From(1),
            false);
        p.SubmitForModeration(Guid.NewGuid());
        return p;
    }
}
