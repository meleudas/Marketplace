using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.Commands.DeleteCategory;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class IntegrationCatalogCategoriesSqliteTests
{
    [Fact]
    public async Task Category_Create_Then_Query_Active_Works_With_Real_Db()
    {
        await using var db = await CreateSqliteContextAsync();
        var categoryRepo = new CategoryRepository(db);
        var cache = new NoopCachePort();

        var create = new CreateCategoryCommandHandler(categoryRepo, cache);
        var createResult = await create.Handle(
            new CreateCategoryCommand("Peripherals", "peripherals", null, null, null, null, 1, true),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var query = new GetActiveCategoriesQueryHandler(categoryRepo, cache, Options.Create(new CacheTtlOptions()));
        var queryResult = await query.Handle(new GetActiveCategoriesQuery(), CancellationToken.None);

        Assert.True(queryResult.IsSuccess);
        Assert.Contains(queryResult.Value!, x => x.Slug == "peripherals");
    }

    [Fact]
    public async Task Search_Falls_Back_To_Db_When_SearchService_Fails()
    {
        await using var db = await CreateSqliteContextAsync();
        var now = DateTime.UtcNow;
        var companyId = Guid.NewGuid();
        const long categoryId = 50;
        const long productId = 777;

        var categoryRepo = new CategoryRepository(db);
        await categoryRepo.AddAsync(
            Marketplace.Domain.Categories.Entities.Category.Create(
                CategoryId.From(categoryId),
                "Accessories",
                "accessories",
                null,
                null,
                null,
                JsonBlob.Empty,
                0,
                true),
            CancellationToken.None);

        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(
            Product.Reconstitute(
                ProductId.From(productId),
                CompanyId.From(companyId),
                "Mechanical Keyboard",
                "mechanical-keyboard",
                "RGB keyboard",
                new Money(250),
                null,
                5,
                1,
                CategoryId.From(categoryId),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                now,
                now,
                false,
                null),
            CancellationToken.None);

        var stockRepo = new WarehouseStockRepository(db);
        await stockRepo.AddAsync(
            WarehouseStock.Reconstitute(
                WarehouseStockId.From(1),
                CompanyId.From(companyId),
                WarehouseId.From(1),
                ProductId.From(productId),
                10,
                0,
                0,
                1,
                now,
                now,
                false,
                null),
            CancellationToken.None);

        var handler = new SearchCatalogProductsQueryHandler(
            new FailingSearchService(),
            productRepo,
            stockRepo,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("keyboard", null, [categoryId], companyId, 100, 500, "in_stock", "price_asc", 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("mechanical-keyboard", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task DeleteCategory_Fails_When_Category_Has_Active_Products_With_Real_Db()
    {
        await using var db = await CreateSqliteContextAsync();
        var now = DateTime.UtcNow;
        var companyId = Guid.NewGuid();
        const long categoryId = 90;

        var categoryRepo = new CategoryRepository(db);
        await categoryRepo.AddAsync(
            Marketplace.Domain.Categories.Entities.Category.Create(
                CategoryId.From(categoryId),
                "Storage",
                "storage",
                null,
                null,
                null,
                JsonBlob.Empty,
                0,
                true),
            CancellationToken.None);

        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(
            Product.Reconstitute(
                ProductId.From(900),
                CompanyId.From(companyId),
                "SSD",
                "ssd",
                "fast",
                new Money(99),
                null,
                10,
                0,
                CategoryId.From(categoryId),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                now,
                now,
                false,
                null),
            CancellationToken.None);

        var handler = new DeleteCategoryCommandHandler(categoryRepo, productRepo, new NoopCachePort());
        var result = await handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("active products", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
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

    private sealed class FailingSearchService : IProductSearchService
    {
        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(string? name, IReadOnlyList<long>? categoryIds, Guid? companyId, decimal? minPrice, decimal? maxPrice, string? availabilityStatus, string? sort, int page, int pageSize, string? searchAfter, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch unavailable"));
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }
}
