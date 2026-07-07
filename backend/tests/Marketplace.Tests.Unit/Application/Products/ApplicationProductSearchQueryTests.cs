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
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public class ApplicationProductSearchQueryTests
{
    [Fact]
    public async Task Uses_Elasticsearch_Result_When_Available()
    {
        var service = new StubSearchService(Result<ProductSearchResultDto>.Success(
            new ProductSearchResultDto(
                [new ProductListItemDto(1, Guid.NewGuid(), "A", "a", "d", 10, null, null, 1, "active", false, 0, 0, 0, "out_of_stock", DateTime.UtcNow, DateTime.UtcNow, [])],
                1, 1, 20)));
        var handler = new SearchCatalogProductsQueryHandler(
            service,
            new InMemoryProductRepository(),
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            new InMemoryWarehouseStockRepository(),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("a", null, null, null, null, null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

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
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("key", null, null, null, 50, 200, null, null, null, null, null, "price_asc", 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("keyboard", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_And_Finds_Product_By_Fuzzy_Name()
    {
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(12),
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
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(Guid.NewGuid()), WarehouseId.From(1), ProductId.From(12), 5, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("keyboar", null, null, null, null, null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("keyboard", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_When_Elasticsearch_Throws()
    {
        var products = new InMemoryProductRepository();
        var companyId = Guid.NewGuid();
        products.Seed(Product.Reconstitute(
            ProductId.From(11),
            CompanyId.From(companyId),
            "Mouse",
            "mouse",
            "Gaming mouse",
            new Money(80),
            null,
            2,
            0,
            CategoryId.From(6),
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
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(11), 2, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new ThrowingSearchService(),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("mouse", null, [6], companyId, 10, 200, "low_stock", null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("mouse", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Db_Fallback_Filters_By_Book_Facets()
    {
        var products = new InMemoryProductRepository();
        var companyId = Guid.NewGuid();
        products.Seed(Product.Reconstitute(
            ProductId.From(20),
            CompanyId.From(companyId),
            "The Hobbit",
            "the-hobbit",
            "Fantasy book",
            new Money(25),
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
            null));
        products.Seed(Product.Reconstitute(
            ProductId.From(21),
            CompanyId.From(companyId),
            "Clean Code",
            "clean-code",
            "Programming book",
            new Money(40),
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
            null));

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            ProductId.From(20),
            "the-hobbit",
            new JsonBlob("""{"author":"Tolkien","format":"hardcover","genre":"fantasy"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(2),
            ProductId.From(21),
            "clean-code",
            new JsonBlob("""{"author":"Martin","format":"paperback","genre":"tech"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(20), 3, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(2), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(21), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            details,
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, ["Tolkien"], "hardcover", ["fantasy"], null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("the-hobbit", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_And_Filters_By_Any_Of_Multiple_Authors()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(20),
            CompanyId.From(companyId),
            "The Hobbit",
            "the-hobbit",
            "Fantasy book",
            new Money(30),
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
            null));
        products.Seed(Product.Reconstitute(
            ProductId.From(21),
            CompanyId.From(companyId),
            "Clean Code",
            "clean-code",
            "Programming book",
            new Money(40),
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
            null));
        products.Seed(Product.Reconstitute(
            ProductId.From(22),
            CompanyId.From(companyId),
            "Harry Potter",
            "harry-potter",
            "Fantasy book",
            new Money(25),
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
            null));

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            ProductId.From(20),
            "the-hobbit",
            new JsonBlob("""{"author":"Tolkien"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(2),
            ProductId.From(21),
            "clean-code",
            new JsonBlob("""{"author":"Martin"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(3),
            ProductId.From(22),
            "harry-potter",
            new JsonBlob("""{"author":"Rowling"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(20), 3, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(2), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(21), 3, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(3), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(22), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            details,
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, ["Tolkien", "Martin"], null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Contains(result.Value.Items, x => x.Slug == "the-hobbit");
        Assert.Contains(result.Value.Items, x => x.Slug == "clean-code");
    }

    [Fact]
    public async Task Falls_Back_To_Db_And_Filters_By_Genre_Tag()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(20),
            CompanyId.From(companyId),
            "The Hobbit",
            "the-hobbit",
            "Fantasy book",
            new Money(30),
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
            null));
        products.Seed(Product.Reconstitute(
            ProductId.From(21),
            CompanyId.From(companyId),
            "Clean Code",
            "clean-code",
            "Programming book",
            new Money(40),
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
            null));

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            ProductId.From(20),
            "the-hobbit",
            new JsonBlob("""{"author":"Tolkien"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            ["фентезі", "популярне"],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(2),
            ProductId.From(21),
            "clean-code",
            new JsonBlob("""{"author":"Martin","genre":"it"}"""),
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(20), 3, 0, 0));
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(2), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(21), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            details,
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var fantasyResult = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, null, null, ["фентезі"], null, null, 1, 20, null),
            CancellationToken.None);
        var itResult = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, null, null, ["it"], null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(fantasyResult.IsSuccess);
        Assert.Single(fantasyResult.Value!.Items);
        Assert.Equal("the-hobbit", fantasyResult.Value.Items[0].Slug);

        Assert.True(itResult.IsSuccess);
        Assert.Single(itResult.Value!.Items);
        Assert.Equal("clean-code", itResult.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_When_Elasticsearch_Returns_Empty_Facet_Results()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(30),
            CompanyId.From(companyId),
            "Солодка Даруся",
            "solodka-darusia",
            "Книга",
            new Money(320),
            null,
            5,
            0,
            CategoryId.From(11),
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

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            ProductId.From(30),
            "solodka-darusia",
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            ["Марія Матіос"],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(30), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Success(new ProductSearchResultDto([], 0, 1, 20))),
            products,
            details,
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, ["Марія Матіос"], null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("solodka-darusia", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_When_Elasticsearch_Returns_Empty_Name_Search_Results()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(32),
            CompanyId.From(companyId),
            "Кобзар",
            "kobzar",
            "Книга",
            new Money(250),
            null,
            5,
            0,
            CategoryId.From(11),
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
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(32), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Success(new ProductSearchResultDto([], 0, 1, 20))),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery("к", null, null, null, null, null, null, null, null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("kobzar", result.Value.Items[0].Slug);
    }

    [Fact]
    public async Task Falls_Back_To_Db_And_Filters_By_Author_In_Brands()
    {
        var companyId = Guid.NewGuid();
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(31),
            CompanyId.From(companyId),
            "Солодка Даруся",
            "solodka-darusia-brands",
            "Книга",
            new Money(320),
            null,
            5,
            0,
            CategoryId.From(11),
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

        var details = new InMemoryProductDetailRepository();
        details.Seed(ProductDetail.Reconstitute(
            ProductDetailId.From(1),
            ProductId.From(31),
            "solodka-darusia-brands",
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            JsonBlob.Empty,
            [],
            ["Марія Матіос"],
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));

        var stocks = new InMemoryWarehouseStockRepository();
        stocks.Seed(WarehouseStock.Create(WarehouseStockId.From(1), CompanyId.From(companyId), WarehouseId.From(1), ProductId.From(31), 3, 0, 0));

        var handler = new SearchCatalogProductsQueryHandler(
            new StubSearchService(Result<ProductSearchResultDto>.Failure("Elasticsearch down")),
            products,
            details,
            new InMemoryProductImageRepository(),
            stocks,
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            new SearchCatalogProductsQuery(null, null, null, null, null, null, null, ["Марія Матіос"], null, null, null, null, 1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("solodka-darusia-brands", result.Value.Items[0].Slug);
    }

    private sealed class StubSearchService : IProductSearchService
    {
        private readonly Result<ProductSearchResultDto> _result;

        public StubSearchService(Result<ProductSearchResultDto> result) => _result = result;

        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
            CatalogProductSearchFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(_result);

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(
            CatalogOnSaleProductFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(_result);

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductSearchServiceFakeMethods.SearchCatalogBrowsableUnavailableAsync(ct);

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => Marketplace.Tests.Common.Fakes.ProductSearchServiceFakeMethods.SearchCatalogBrowsableUnavailableAsync(ct);
    }

    private sealed class ThrowingSearchService : IProductSearchService
    {
        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
            CatalogProductSearchFilters filters,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Elasticsearch unavailable");

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(
            CatalogOnSaleProductFilters filters,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Elasticsearch unavailable");

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Elasticsearch unavailable");

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Elasticsearch unavailable");
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
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.PendingReview && !x.IsDeleted).ToList());

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
