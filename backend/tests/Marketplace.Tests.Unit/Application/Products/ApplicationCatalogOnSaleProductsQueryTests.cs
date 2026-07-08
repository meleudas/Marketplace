using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.ListCatalogOnSaleProducts;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class ApplicationCatalogOnSaleProductsQueryTests
{
    [Fact]
    public async Task Uses_Elasticsearch_Result_When_Available()
    {
        var service = new StubOnSaleSearchService(Result<ProductSearchResultDto>.Success(
            new ProductSearchResultDto(
                [new ProductListItemDto(1, Guid.NewGuid(), "Sale", "sale", "d", 80, 100, 20, 1, "active", false, 0, 0, 5, "in_stock", DateTime.UtcNow, DateTime.UtcNow, [])],
                1, 1, 20)));

        var handler = CreateHandler(service, new InMemoryProductRepository(), new InMemoryWarehouseStockRepository());
        var result = await handler.Handle(new ListCatalogOnSaleProductsQuery(null, null, null, null, null, null, null, 1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(20, result.Value.Items[0].DiscountPercent);
    }

    [Fact]
    public async Task Falls_Back_To_Db_And_Returns_Only_On_Sale_Products()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(CreateProduct(1, companyId, "Regular", 100, null));
        products.Seed(CreateProduct(2, companyId, "Discounted", 80, 100));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(2), 5, 0, 0));

        var handler = CreateHandler(
            new FailingOnSaleSearchService(),
            products,
            stocks);

        var result = await handler.Handle(new ListCatalogOnSaleProductsQuery(null, null, null, null, null, null, "discount_desc", 1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Discounted", result.Value.Items[0].Name);
        Assert.Equal(80, result.Value.Items[0].Price);
        Assert.Equal(100, result.Value.Items[0].OldPrice);
        Assert.Equal(20, result.Value.Items[0].DiscountPercent);
    }

    [Fact]
    public async Task Db_Fallback_Filters_By_Min_Discount_Percent()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(CreateProduct(1, companyId, "Small sale", 90, 100));
        products.Seed(CreateProduct(2, companyId, "Big sale", 50, 100));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(1), 5, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(2), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(2), 5, 0, 0));

        var handler = CreateHandler(new FailingOnSaleSearchService(), products, stocks);
        var result = await handler.Handle(new ListCatalogOnSaleProductsQuery(null, null, null, null, 30, null, null, 1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Big sale", result.Value.Items[0].Name);
    }

    private static ListCatalogOnSaleProductsQueryHandler CreateHandler(
        IProductSearchService searchService,
        InMemoryProductRepository productRepository,
        InMemoryWarehouseStockRepository stockRepository) =>
        new(
            searchService,
            productRepository,
            new InMemoryProductImageRepository(),
            stockRepository,
            new EmptyCategoryRepository(),
            NullLogger<ListCatalogOnSaleProductsQueryHandler>.Instance);

    private sealed class EmptyCategoryRepository : ICategoryRepository
    {
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default) => Task.FromResult<Category?>(null);
        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<Category> AddAsync(Category category, CancellationToken ct = default) => Task.FromResult(category);
        public Task UpdateAsync(Category category, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static Product CreateProduct(long id, Guid companyId, string name, decimal price, decimal? oldPrice) =>
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
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private sealed class StubOnSaleSearchService : IProductSearchService
    {
        private readonly Result<ProductSearchResultDto> _result;

        public StubOnSaleSearchService(Result<ProductSearchResultDto> result) => _result = result;

        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(CatalogProductSearchFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("unused"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(CatalogOnSaleProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(_result);

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductSearchServiceFakeMethods.SearchCatalogBrowsableUnavailableAsync(ct);

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductSearchServiceFakeMethods.SearchCatalogBrowsableUnavailableAsync(ct);
    }

    private sealed class FailingOnSaleSearchService : IProductSearchService
    {
        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(CatalogProductSearchFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("unused"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(CatalogOnSaleProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch down"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch down"));

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(CatalogBrowsableProductFilters filters, CancellationToken ct = default)
            => Task.FromResult(Result<ProductSearchResultDto>.Failure("Elasticsearch down"));
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = new();

        public void Seed(Product product) => _items[product.Id.Value] = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
            => Task.FromResult<Product?>(null);

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Product?>(null);

        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.Active).ToList());

        public Task<IReadOnlyList<Product>> ListActiveOnSaleAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minDiscountPercent = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActiveOnSaleAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, minDiscountPercent, ct);

        public Task<IReadOnlyList<Product>> ListActiveNewestAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActiveNewestAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, ct);

        public Task<IReadOnlyList<Product>> ListActivePopularAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductRepositoryFakeMethods.ListActivePopularAsync(_items.Values, companyId, categoryIds, minPrice, maxPrice, ct);

        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            Seed(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default)
        {
            Seed(product);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductImageRepository : IProductImageRepository
    {
        public Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ProductImage>>([]);

        public Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> ListImageUrlsByProductIdsAsync(
            IReadOnlyCollection<long> productIds,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>(new Dictionary<long, IReadOnlyList<string>>());

        public Task ReplaceForProductAsync(ProductId productId, IReadOnlyList<ProductImage> images, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class InMemoryWarehouseStockRepository : IWarehouseStockRepository
    {
        private readonly List<WarehouseStock> _items = [];

        public void Seed(WarehouseStock item) => _items.Add(item);

        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.WarehouseId == warehouseId && x.ProductId == productId));

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WarehouseStock>>(_items.Where(x => x.CompanyId == companyId).ToList());

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WarehouseStock>>(_items.Where(x => x.ProductId == productId).ToList());

        public Task<IReadOnlyList<WarehouseStock>> ListByWarehouseAsync(WarehouseId warehouseId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WarehouseStock>>([]);

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default)
        {
            _items.Add(stock);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }
}
