using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Commands.CreateProductReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Reviews;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Reviews")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ReviewsModerationPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ReviewsModerationPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_Moderate_And_Delete_Review_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await SeedProductAsync(db);

        var reviewRepo = new ProductReviewRepository(db);
        var create = new CreateProductReviewCommandHandler(
            new ProductRepository(db), reviewRepo, new AlwaysVerifiedPurchaseService(), new NoopRatingAggregationService(), new NoopCachePort(),
            AntiAbuseTestDoubles.PermissiveReviewCreate());
        var delete = new DeleteOwnProductReviewCommandHandler(reviewRepo, new NoopRatingAggregationService(), new NoopCachePort());
        var moderate = new ModerateProductReviewCommandHandler(reviewRepo, new ReviewReplyRepository(db), new NoopRatingAggregationService(), new NoopCachePort());

        var actor = Guid.NewGuid();
        var created = await create.Handle(new CreateProductReviewCommand(product.Id.Value, actor, "buyer", null, 5, "title", "great"), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var hidden = await moderate.Handle(new ModerateProductReviewCommand(created.Value!.Id, Guid.NewGuid(), true, ReviewModerationStatus.Hidden), CancellationToken.None);
        Assert.True(hidden.IsSuccess);

        var deleted = await delete.Handle(new DeleteOwnProductReviewCommand(product.Id.Value, created.Value.Id, actor), CancellationToken.None);
        Assert.True(deleted.IsSuccess);
    }

    private static async Task<Marketplace.Domain.Catalog.Entities.Product> SeedProductAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        db.Categories.Add(new Marketplace.Infrastructure.Persistence.Entities.CategoryRecord
        {
            Id = 501, Name = "Reviews", Slug = "reviews-cat", IsActive = true, CreatedAt = now, UpdatedAt = now, IsDeleted = false
        });
        await db.SaveChangesAsync();

        var repo = new ProductRepository(db);
        var product = Marketplace.Domain.Catalog.Entities.Product.Reconstitute(
            ProductId.From(75001), CompanyId.From(Guid.NewGuid()), "Review Product", "review-product", "d",
            new Money(99), null, 501, 0, CategoryId.From(501), Marketplace.Domain.Catalog.Enums.ProductStatus.Active,
            null, 0, 0, 0, false, now, now, false, null);
        await repo.AddAsync(product, CancellationToken.None);
        return product;
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class AlwaysVerifiedPurchaseService : IReviewPurchaseVerificationService
    {
        public Task<long?> GetVerifiedProductOrderIdAsync(Guid userId, ProductId productId, CancellationToken ct = default) => Task.FromResult<long?>(100);
        public Task<long?> GetVerifiedCompanyOrderIdAsync(Guid userId, CompanyId companyId, CancellationToken ct = default) => Task.FromResult<long?>(100);
    }

    private sealed class NoopRatingAggregationService : IReviewRatingAggregationService
    {
        public Task RecalculateProductAsync(ProductId productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecalculateCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
