using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.GetSimilarProductsById;
using Marketplace.Application.Products.Queries.GetSimilarProductsBySlug;
using Marketplace.Application.Products.Services;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class ApplicationSimilarProductsTests
{
    [Fact]
    public void ScoreCandidate_Prefers_Tag_And_Brand_Overlap()
    {
        var score = SimilarProductsOrchestrator.ScoreCandidate(
            "Gaming Keyboard",
            ["gaming", "rgb"],
            ["logitech"],
            100m,
            Product.Reconstitute(
                ProductId.From(2),
                CompanyId.From(Guid.NewGuid()),
                "RGB Keyboard",
                "rgb-keyboard",
                "Mechanical keyboard",
                new Money(105),
                null,
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
                null),
            ProductDetail.Reconstitute(
                ProductDetailId.From(1),
                ProductId.From(2),
                "rgb-keyboard",
                JsonBlob.Empty,
                JsonBlob.Empty,
                JsonBlob.Empty,
                JsonBlob.Empty,
                JsonBlob.Empty,
                ["gaming", "rgb"],
                ["logitech"],
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null));

        Assert.True(score >= 7);
    }

    [Fact]
    public async Task GetSimilarProductsBySlug_Returns_NotFound_For_Missing_Product()
    {
        var handler = new GetSimilarProductsBySlugQueryHandler(CreateOrchestrator(new InMemoryProductRepository(), new StubSimilarityService(Result<SimilarProductsResultDto>.Success(new SimilarProductsResultDto(1, [])))));

        var result = await handler.Handle(new GetSimilarProductsBySlugQuery("missing", 12), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Falls_Back_To_Db_When_Elasticsearch_Fails()
    {
        var products = new InMemoryProductRepository();
        var companyId = Guid.NewGuid();
        var source = SeedProduct(products, 1, companyId, "source-keyboard", "Source Keyboard", 100, 5, ["gaming"]);
        SeedProduct(products, 2, companyId, "similar-keyboard", "Similar Gaming Keyboard", 110, 5, ["gaming"]);
        SeedProduct(products, 3, companyId, "other-category", "Other item", 90, 6, ["gaming"]);

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            source.Id,
            source.Slug,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            ["gaming"],
            ["acme"],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(2),
            ProductId.From(2),
            "similar-keyboard",
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            ["gaming"],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), source.Id, 5, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(2), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(2), 3, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(3), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(3), 3, 0, 0));

        var orchestrator = CreateOrchestrator(
            products,
            new StubSimilarityService(Result<SimilarProductsResultDto>.Failure("Elasticsearch down")),
            details,
            stocks);

        var handler = new GetSimilarProductsByIdQueryHandler(orchestrator);
        var result = await handler.Handle(new GetSimilarProductsByIdQuery(1, 12), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("similar-keyboard", result.Value.Items[0].Slug);
        Assert.Equal(1, result.Value.SourceProductId);
    }

    private static SimilarProductsOrchestrator CreateOrchestrator(
        InMemoryProductRepository products,
        IProductSimilarityService similarityService,
        InMemoryProductDetailRepository? details = null,
        InMemoryWarehouseStockRepository? stocks = null)
    {
        return new SimilarProductsOrchestrator(
            products,
            details ?? new InMemoryProductDetailRepository(),
            stocks ?? new InMemoryWarehouseStockRepository(),
            similarityService,
            new NoopCache(),
            Options.Create(new SimilarProductsOptions()),
            Options.Create(new CacheTtlOptions()),
            NullLogger<SimilarProductsOrchestrator>.Instance);
    }

    private static Product SeedProduct(
        InMemoryProductRepository products,
        long id,
        Guid companyId,
        string slug,
        string name,
        decimal price,
        long categoryId,
        string[] tags)
    {
        var product = Product.Reconstitute(
            ProductId.From(id),
            CompanyId.From(companyId),
            name,
            slug,
            $"{name} description",
            new Money(price),
            null,
            5,
            0,
            CategoryId.From(categoryId),
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
        products.Seed(product);
        return product;
    }

    private sealed class StubSimilarityService : IProductSimilarityService
    {
        private readonly Result<SimilarProductsResultDto> _result;

        public StubSimilarityService(Result<SimilarProductsResultDto> result) => _result = result;

        public Task<Result<SimilarProductsResultDto>> GetSimilarProductsAsync(
            long productId,
            long categoryId,
            string name,
            string description,
            IReadOnlyList<string> tags,
            IReadOnlyList<string> brands,
            decimal price,
            int limit,
            CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private sealed class NoopCache : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = new();

        public void Seed(Product product) => _items[product.Id.Value] = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.CompanyId == companyId && x.Slug == slug));

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.Slug == slug));

        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
        {
            var set = ids.Select(x => x.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => set.Contains(x.Id.Value)).ToList());
        }

        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.CompanyId == companyId).ToList());

        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.Active && !x.IsDeleted).ToList());

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

    private sealed class InMemoryProductDetailRepository : IProductDetailRepository
    {
        private readonly Dictionary<long, ProductDetail> _items = new();

        public void Seed(ProductDetail detail) => _items[detail.ProductId.Value] = detail;

        public Task<ProductDetail?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(productId.Value));

        public Task UpsertAsync(ProductDetail detail, CancellationToken ct = default)
        {
            Seed(detail);
            return Task.CompletedTask;
        }

        public Task AddAsync(ProductDetail detail, CancellationToken ct = default)
        {
            Seed(detail);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductDetail detail, CancellationToken ct = default)
        {
            Seed(detail);
            return Task.CompletedTask;
        }
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
            => Task.FromResult<IReadOnlyList<WarehouseStock>>(_items.Where(x => x.CompanyId == companyId && x.ProductId == productId).ToList());

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default)
        {
            _items.Add(stock);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
