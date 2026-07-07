using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.ListCatalogNewProducts;
using Marketplace.Application.Products.Queries.ListCatalogPopularProducts;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class ApplicationCatalogBrowseProductsQueryTests
{
    [Fact]
    public async Task New_Products_Fallback_Orders_By_CreatedAt_Desc()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(CreateProduct(1, companyId, "Older", 100, null, DateTime.UtcNow.AddDays(-2), 0, 0));
        products.Seed(CreateProduct(2, companyId, "Newer", 100, null, DateTime.UtcNow, 0, 0));

        var handler = CreateNewHandler(new FailingBrowseSearchService(), products, new InMemoryWarehouseStockRepository());
        var result = await handler.Handle(new ListCatalogNewProductsQuery(null, null, null, null, null, 1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal("Newer", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Popular_Products_Fallback_Orders_By_Sales_And_Views()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(CreateProduct(1, companyId, "Low", 100, null, DateTime.UtcNow, 1, 1));
        products.Seed(CreateProduct(2, companyId, "High", 100, null, DateTime.UtcNow, 50, 10));

        var handler = CreatePopularHandler(new FailingBrowseSearchService(), products, new InMemoryWarehouseStockRepository());
        var result = await handler.Handle(new ListCatalogPopularProductsQuery(null, null, null, null, null, 1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal("High", result.Value.Items[0].Name);
    }

    private static ListCatalogNewProductsQueryHandler CreateNewHandler(
        IProductSearchService searchService,
        InMemoryProductRepository productRepository,
        InMemoryWarehouseStockRepository stockRepository) =>
        new(searchService, productRepository, new InMemoryProductImageRepository(), stockRepository, NullLogger<ListCatalogNewProductsQueryHandler>.Instance);

    private static ListCatalogPopularProductsQueryHandler CreatePopularHandler(
        IProductSearchService searchService,
        InMemoryProductRepository productRepository,
        InMemoryWarehouseStockRepository stockRepository) =>
        new(searchService, productRepository, new InMemoryProductImageRepository(), stockRepository, NullLogger<ListCatalogPopularProductsQueryHandler>.Instance);

    private static Product CreateProduct(
        long id,
        Guid companyId,
        string name,
        decimal price,
        decimal? oldPrice,
        DateTime createdAt,
        long salesCount,
        long viewCount) =>
        Product.Reconstitute(
            ProductId.From(id),
            CompanyId.From(companyId),
            name,
            name.ToLowerInvariant(),
            "description",
            new Money(price),
            oldPrice.HasValue ? new Money(oldPrice.Value) : null,
            5,
            0,
            CategoryId.From(1),
            ProductStatus.Active,
            null,
            0,
            viewCount,
            salesCount,
            false,
            createdAt,
            createdAt,
            false,
            null);

    private sealed class FailingBrowseSearchService : IProductSearchService
    {
        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(CatalogProductSearchFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("unused"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(CatalogOnSaleProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("unused"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch down"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch down"));
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = new();

        public void Seed(Product product) => _items[product.Id.Value] = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default) => Task.FromResult<Product?>(null);
        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult<Product?>(null);
        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>(_items.Values.ToList());
        public Task<IReadOnlyList<Product>> ListActiveOnSaleAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minDiscountPercent = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActiveOnSaleAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, minDiscountPercent, ct);
        public Task<IReadOnlyList<Product>> ListActiveNewestAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActiveNewestAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, ct);
        public Task<IReadOnlyList<Product>> ListActivePopularAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActivePopularAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, ct);
        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task AddAsync(Product product, CancellationToken ct = default) { Seed(product); return Task.CompletedTask; }
        public Task UpdateAsync(Product product, CancellationToken ct = default) { Seed(product); return Task.CompletedTask; }
    }

    private sealed class InMemoryProductImageRepository : IProductImageRepository
    {
        public Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(ProductId productId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ProductImage>>([]);
        public Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> ListImageUrlsByProductIdsAsync(IReadOnlyCollection<long> productIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>(new Dictionary<long, IReadOnlyList<string>>());
        public Task ReplaceForProductAsync(ProductId productId, IReadOnlyList<ProductImage> images, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryWarehouseStockRepository : IWarehouseStockRepository
    {
        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default) => Task.FromResult<WarehouseStock?>(null);
        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<WarehouseStock>>([]);
        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<WarehouseStock>>([]);
        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }
}
