using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.CreateProduct;
using Marketplace.Application.Products.Commands.RejectProduct;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.GetPendingProducts;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "ProductsModeration")]
public sealed class IntegrationProductsModerationSqliteTests
{
    [Fact]
    public async Task Create_Approve_Publishes_To_Catalog_Search()
    {
        await using var db = await CreateSqliteContextAsync();
        await SeedCategoryAsync(db, 5);

        var companyId = Guid.NewGuid();
        var products = new ProductRepository(db);
        var createHandler = new CreateProductCommandHandler(
            new AllowAccessService(),
            products,
            new ProductDetailRepository(db),
            new ProductImageRepository(db),
            new NoopCachePort(),
            new NoopAppNotifications());
        var approveHandler = new ApproveProductCommandHandler(
            products,
            new NoopCachePort(),
            new SpySearchDispatcher(),
            new NoopAppNotifications());
        var searchHandler = new SearchCatalogProductsQueryHandler(
            new FailingSearchService(),
            products,
            new WarehouseStockRepository(db),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var create = await createHandler.Handle(
            new CreateProductCommand(
                companyId,
                Guid.NewGuid(),
                false,
                "Keyboard",
                "kbd",
                "Gaming keyboard",
                200,
                null,
                0,
                5,
                false,
                null,
                null),
            CancellationToken.None);
        var beforeApprove = await searchHandler.Handle(
            new SearchCatalogProductsQuery("keyboard", null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);
        var approve = await approveHandler.Handle(new ApproveProductCommand(create.Value!.Product.Id, Guid.NewGuid()), CancellationToken.None);
        var afterApprove = await searchHandler.Handle(
            new SearchCatalogProductsQuery("keyboard", null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(create.IsSuccess);
        Assert.True(approve.IsSuccess);
        Assert.True(beforeApprove.IsSuccess);
        Assert.Empty(beforeApprove.Value!.Items);
        Assert.True(afterApprove.IsSuccess);
        Assert.Single(afterApprove.Value!.Items);
        Assert.Equal("kbd", afterApprove.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Reject_Keeps_Product_Out_Of_Published_Catalog()
    {
        await using var db = await CreateSqliteContextAsync();
        await SeedCategoryAsync(db, 7);

        var products = new ProductRepository(db);
        var createHandler = new CreateProductCommandHandler(
            new AllowAccessService(),
            products,
            new ProductDetailRepository(db),
            new ProductImageRepository(db),
            new NoopCachePort(),
            new NoopAppNotifications());
        var rejectHandler = new RejectProductCommandHandler(
            products,
            new NoopCachePort(),
            new NoopAppNotifications());
        var pendingQuery = new GetPendingProductsQueryHandler(products);
        var searchHandler = new SearchCatalogProductsQueryHandler(
            new FailingSearchService(),
            products,
            new WarehouseStockRepository(db),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var create = await createHandler.Handle(
            new CreateProductCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                false,
                "Headset",
                "headset",
                "Studio headset",
                150,
                null,
                0,
                7,
                false,
                null,
                null),
            CancellationToken.None);
        var pending = await pendingQuery.Handle(new GetPendingProductsQuery(), CancellationToken.None);
        var reject = await rejectHandler.Handle(
            new RejectProductCommand(create.Value!.Product.Id, Guid.NewGuid(), "quality issue"),
            CancellationToken.None);
        var search = await searchHandler.Handle(
            new SearchCatalogProductsQuery("headset", null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(create.IsSuccess);
        Assert.True(pending.IsSuccess);
        Assert.Contains(pending.Value!, x => x.ProductId == create.Value!.Product.Id);
        Assert.True(reject.IsSuccess);
        Assert.True(search.IsSuccess);
        Assert.Empty(search.Value!.Items);
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

    private static async Task SeedCategoryAsync(ApplicationDbContext db, long categoryId)
    {
        var categories = new CategoryRepository(db);
        await categories.AddAsync(
            Category.Create(
                CategoryId.From(categoryId),
                $"Category-{categoryId}",
                $"category-{categoryId}",
                null,
                null,
                null,
                JsonBlob.Empty,
                0,
                true),
            CancellationToken.None);
    }

    private sealed class AllowAccessService : IProductAccessService
    {
        public Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, ProductPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class FailingSearchService : IProductSearchService
    {
        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
            string? name,
            IReadOnlyList<long>? categoryIds,
            Guid? companyId,
            decimal? minPrice,
            decimal? maxPrice,
            string? availabilityStatus,
            string? sort,
            int page,
            int pageSize,
            string? searchAfter,
            CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("search unavailable"));
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopAppNotifications : IAppNotificationScheduler
    {
        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SpySearchDispatcher : IProductSearchIndexDispatcher
    {
        public Task EnqueueUpsertProductAsync(long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnqueueDeleteProductAsync(long productId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
