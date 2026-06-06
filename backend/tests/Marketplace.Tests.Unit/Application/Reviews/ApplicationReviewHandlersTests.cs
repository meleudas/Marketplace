using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Authorization;
using Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;
using Marketplace.Application.Reviews.Commands.UpsertProductReviewReply;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Domain.Reviews.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Reviews")]
public sealed class ApplicationReviewHandlersTests
{
    [Fact]
    public async Task UpdateOwnProductReview_Returns_NotFound_On_ProductRouteMismatch()
    {
        var review = BuildProductReview(productId: 11, userId: Guid.NewGuid());
        var repo = new InMemoryProductReviewRepository(review);
        var handler = new UpdateOwnProductReviewCommandHandler(
            repo,
            new InMemoryReviewReplyRepository(),
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new UpdateOwnProductReviewCommand(22, review.Id.Value, review.UserId, 5, "t", "updated"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Review not found", result.Error);
    }

    [Fact]
    public async Task DeleteOwnProductReview_Returns_NotFound_On_ProductRouteMismatch()
    {
        var review = BuildProductReview(productId: 11, userId: Guid.NewGuid());
        var repo = new InMemoryProductReviewRepository(review);
        var handler = new DeleteOwnProductReviewCommandHandler(
            repo,
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new DeleteOwnProductReviewCommand(22, review.Id.Value, review.UserId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Review not found", result.Error);
    }

    [Fact]
    public async Task UpdateOwnCompanyReview_Returns_NotFound_On_CompanyRouteMismatch()
    {
        var review = BuildCompanyReview(companyId: Guid.NewGuid(), userId: Guid.NewGuid());
        var repo = new InMemoryCompanyReviewRepository(review);
        var handler = new UpdateOwnCompanyReviewCommandHandler(
            repo,
            new InMemoryReviewReplyRepository(),
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new UpdateOwnCompanyReviewCommand(Guid.NewGuid(), review.Id.Value, review.UserId, 4.5m, "updated"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Review not found", result.Error);
    }

    [Fact]
    public async Task DeleteOwnCompanyReview_Returns_NotFound_On_CompanyRouteMismatch()
    {
        var review = BuildCompanyReview(companyId: Guid.NewGuid(), userId: Guid.NewGuid());
        var repo = new InMemoryCompanyReviewRepository(review);
        var handler = new DeleteOwnCompanyReviewCommandHandler(
            repo,
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new DeleteOwnCompanyReviewCommand(Guid.NewGuid(), review.Id.Value, review.UserId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Review not found", result.Error);
    }

    [Fact]
    public async Task ModerateProductReview_Returns_Forbidden_Without_Moderation_Access()
    {
        var handler = new ModerateProductReviewCommandHandler(
            new InMemoryProductReviewRepository(BuildProductReview(11, Guid.NewGuid())),
            new InMemoryReviewReplyRepository(),
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new ModerateProductReviewCommand(1, Guid.NewGuid(), false, ReviewModerationStatus.Hidden),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task UpsertProductReply_Returns_Forbidden_Without_Company_Access()
    {
        var review = BuildProductReview(productId: 11, userId: Guid.NewGuid());
        var products = new InMemoryCatalogProductRepository(
            Marketplace.Domain.Catalog.Entities.Product.Reconstitute(
                ProductId.From(11),
                CompanyId.From(Guid.NewGuid()),
                "N",
                "slug",
                "d",
                new Money(10),
                null,
                0,
                0,
                CategoryId.From(1),
                Marketplace.Domain.Catalog.Enums.ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null));
        var handler = new UpsertProductReviewReplyCommandHandler(
            new InMemoryProductReviewRepository(review),
            products,
            new InMemoryReviewReplyRepository(),
            new DenyReviewAccessService(),
            new NoopCachePort());

        var result = await handler.Handle(
            new UpsertProductReviewReplyCommand(review.Id.Value, Guid.NewGuid(), false, "reply"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    private static ProductReview BuildProductReview(long productId, Guid userId)
        => ProductReview.Reconstitute(
            ProductReviewId.From(1),
            ProductId.From(productId),
            userId,
            "user",
            null,
            5,
            "title",
            "comment",
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            true,
            OrderId.From(1),
            JsonBlob.Empty,
            ReviewModerationStatus.Approved,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private static CompanyReview BuildCompanyReview(Guid companyId, Guid userId)
        => CompanyReview.Reconstitute(
            CompanyReviewId.From(1),
            CompanyId.From(companyId),
            userId,
            "user",
            1,
            true,
            JsonBlob.Empty,
            5,
            "comment",
            CompanyReviewStatus.Approved,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopRatingAggregationService : IReviewRatingAggregationService
    {
        public Task RecalculateProductAsync(ProductId productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecalculateCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class DenyReviewAccessService : IReviewAccessService
    {
        public Task<bool> HasCompanyAccessAsync(CompanyId companyId, Guid actorUserId, bool isActorAdmin, ReviewPermission permission, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    private sealed class InMemoryReviewReplyRepository : IReviewReplyRepository
    {
        public Task<ReviewReply?> GetByProductReviewIdAsync(ProductReviewId reviewId, CancellationToken ct = default) => Task.FromResult<ReviewReply?>(null);
        public Task<ReviewReply?> GetByCompanyReviewIdAsync(CompanyReviewId reviewId, CancellationToken ct = default) => Task.FromResult<ReviewReply?>(null);
        public Task<ReviewReply> AddAsync(ReviewReply reply, CancellationToken ct = default) => Task.FromResult(reply);
        public Task UpdateAsync(ReviewReply reply, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductReviewRepository : IProductReviewRepository
    {
        private readonly ProductReview _review;

        public InMemoryProductReviewRepository(ProductReview review) => _review = review;

        public Task<ProductReview?> GetByIdAsync(ProductReviewId id, CancellationToken ct = default)
            => Task.FromResult(id.Value == _review.Id.Value ? _review : null);

        public Task<ProductReview?> GetByProductAndUserAsync(ProductId productId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_review.ProductId == productId && _review.UserId == userId ? _review : null);

        public Task<IReadOnlyList<ProductReview>> ListByProductAsync(ProductId productId, int page, int size, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ProductReview>>([]);

        public Task<(decimal? Average, int Count)> GetApprovedStatsAsync(ProductId productId, CancellationToken ct = default)
            => Task.FromResult<(decimal?, int)>((null, 0));

        public Task<ProductReview> AddAsync(ProductReview review, CancellationToken ct = default) => Task.FromResult(review);
        public Task UpdateAsync(ProductReview review, CancellationToken ct = default) => Task.CompletedTask;
        public Task SoftDeleteAsync(ProductReviewId id, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ProductReview>> ListByStatusAsync(ReviewModerationStatus status, int page, int size, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ProductReview>>([]);
    }

    private sealed class InMemoryCompanyReviewRepository : ICompanyReviewRepository
    {
        private readonly CompanyReview _review;

        public InMemoryCompanyReviewRepository(CompanyReview review) => _review = review;

        public Task<CompanyReview?> GetByIdAsync(CompanyReviewId id, CancellationToken ct = default)
            => Task.FromResult(id.Value == _review.Id.Value ? _review : null);

        public Task<CompanyReview?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_review.CompanyId == companyId && _review.UserId == userId ? _review : null);

        public Task<IReadOnlyList<CompanyReview>> ListByCompanyAsync(CompanyId companyId, int page, int size, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyReview>>([]);

        public Task<(decimal? Average, int Count)> GetApprovedStatsAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<(decimal?, int)>((null, 0));

        public Task<CompanyReview> AddAsync(CompanyReview review, CancellationToken ct = default) => Task.FromResult(review);
        public Task UpdateAsync(CompanyReview review, CancellationToken ct = default) => Task.CompletedTask;
        public Task SoftDeleteAsync(CompanyReviewId id, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<CompanyReview>> ListByStatusAsync(CompanyReviewStatus status, int page, int size, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyReview>>([]);
    }

    private sealed class InMemoryCatalogProductRepository : Marketplace.Domain.Catalog.Repositories.IProductRepository
    {
        private readonly Marketplace.Domain.Catalog.Entities.Product _product;

        public InMemoryCatalogProductRepository(Marketplace.Domain.Catalog.Entities.Product product) => _product = product;

        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult(id.Value == _product.Id.Value ? _product : null);

        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
            => Task.FromResult<Marketplace.Domain.Catalog.Entities.Product?>(null);

        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Marketplace.Domain.Catalog.Entities.Product?>(null);

        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);

        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);

        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);

        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListPendingReviewAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);

        public Task AddAsync(Marketplace.Domain.Catalog.Entities.Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Catalog.Entities.Product product, CancellationToken ct = default) => Task.CompletedTask;
    }
}
