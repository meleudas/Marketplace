using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Tests;

public class ApplicationProductSearchQueryTests
{
    [Fact]
    public async Task Uses_Elasticsearch_Result_When_Available()
    {
        var service = new StubSearchService(Result<ProductSearchResultDto>.Success(
            new ProductSearchResultDto(
                [new ProductListItemDto(1, Guid.NewGuid(), "A", "a", "d", 10, null, 1, "active", false, 0, 0, 0, "out_of_stock", DateTime.UtcNow, DateTime.UtcNow)],
                1, 1, 20)));
        var handler = new SearchCatalogProductsQueryHandler(service, new InMemoryProductRepository(), new InMemoryWarehouseStockRepository());

        var result = await handler.Handle(new SearchCatalogProductsQuery("a", null, null, null, null, null, null, null, 1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }

    [Fact]
    public async Task Falls_Back_To_Db_When_Elasticsearch_Fails()
    {
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(10),
            CompanyId.From(Guid.NewGuid()),
            "Keyboard",
            "keyboard",
            "Gaming keyboard",
            new Money(120),
            null,
            2,
            0,
            CategoryId.From(5),
            ProductStatus.Active,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(Guid.NewGuid()), WarehouseId.From(1), ProductId.From(10), 5, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            stocks);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("key", null, null, null, 50, 200, null, "price_asc", 1, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("keyboard", result.Value.Items[0].Slug);
    }

    private sealed class StubSearchService : IProductSearchService
    {
        private readonly Result<ProductSearchResultDto> _result;

        public StubSearchService(Result<ProductSearchResultDto> result) => _result = result;

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
            CancellationToken ct = default)
            => Task.FromResult(_result);
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
