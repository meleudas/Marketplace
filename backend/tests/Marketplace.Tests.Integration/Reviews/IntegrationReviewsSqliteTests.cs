using Marketplace.Application.Common.Ports;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Application.Reviews.Commands.CreateProductReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Reviews")]
public sealed class IntegrationReviewsSqliteTests
{
    [Fact]
    public async Task ProductReview_Create_Then_List_Works_With_Real_Db()
    {
        await using var db = await CreateSqliteContextAsync();
        var product = await SeedProductAsync(db);
        var reviews = new ProductReviewRepository(db);
        var createHandler = new CreateProductReviewCommandHandler(
            new ProductRepository(db),
            reviews,
            new AlwaysVerifiedPurchaseService(),
            new NoopRatingAggregationService(),
            new NoopCachePort(),
            AntiAbuseTestDoubles.PermissiveReviewCreate());

        var create = await createHandler.Handle(
            new CreateProductReviewCommand(product.Id.Value, Guid.NewGuid(), "buyer", null, 5, "title", "great"),
            CancellationToken.None);
        var listed = await reviews.ListByProductAsync(product.Id, 1, 20, CancellationToken.None);

        Assert.True(create.IsSuccess);
        Assert.Single(listed);
    }

    [Fact]
    public async Task ProductReview_Delete_Allows_Recreate_And_Moderation_Hides_From_List()
    {
        await using var db = await CreateSqliteContextAsync();
        var product = await SeedProductAsync(db);
        var productRepo = new ProductRepository(db);
        var reviewRepo = new ProductReviewRepository(db);
        var createHandler = new CreateProductReviewCommandHandler(
            productRepo,
            reviewRepo,
            new AlwaysVerifiedPurchaseService(),
            new NoopRatingAggregationService(),
            new NoopCachePort(),
            AntiAbuseTestDoubles.PermissiveReviewCreate());
        var deleteHandler = new DeleteOwnProductReviewCommandHandler(
            reviewRepo,
            new NoopRatingAggregationService(),
            new NoopCachePort());
        var moderateHandler = new ModerateProductReviewCommandHandler(
            reviewRepo,
            new ReviewReplyRepository(db),
            new NoopRatingAggregationService(),
            new NoopCachePort());

        var actor = Guid.NewGuid();
        var first = await createHandler.Handle(
            new CreateProductReviewCommand(product.Id.Value, actor, "buyer", null, 5, "title", "first"),
            CancellationToken.None);
        var deleted = await deleteHandler.Handle(
            new DeleteOwnProductReviewCommand(product.Id.Value, first.Value!.Id, actor),
            CancellationToken.None);
        var second = await createHandler.Handle(
            new CreateProductReviewCommand(product.Id.Value, actor, "buyer", null, 4, "title2", "second"),
            CancellationToken.None);
        var hidden = await moderateHandler.Handle(
            new ModerateProductReviewCommand(second.Value!.Id, Guid.NewGuid(), true, ReviewModerationStatus.Hidden),
            CancellationToken.None);
        var listed = await reviewRepo.ListByProductAsync(product.Id, 1, 20, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(hidden.IsSuccess);
        Assert.Empty(listed);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Marketplace.Domain.Catalog.Entities.Product> SeedProductAsync(ApplicationDbContext db)
    {
        const long categoryId = 5;
        var categories = new CategoryRepository(db);
        await categories.AddAsync(
            Category.Create(
                CategoryId.From(categoryId),
                "Cat",
                "cat",
                null,
                null,
                null,
                JsonBlob.Empty,
                0,
                true),
            CancellationToken.None);

        var productRepo = new ProductRepository(db);
        var product = Marketplace.Domain.Catalog.Entities.Product.Reconstitute(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "Product",
            "product",
            "description",
            new Money(10),
            null,
            0,
            0,
            CategoryId.From(categoryId),
            Marketplace.Domain.Catalog.Enums.ProductStatus.Active,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
        await productRepo.AddAsync(product, CancellationToken.None);
        return await productRepo.GetBySlugAsync("product", CancellationToken.None) ?? product;
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

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }
}
