using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Catalog;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Collection(nameof(MarketplaceCatalogSearchContainersCollection))]
[Trait("Suite", "CatalogSearchFilters")]
[Trait("Layer", "IntegrationContainers")]
public sealed class CatalogSearchFilterCombinationContainersTests
{
    private readonly MarketplaceCatalogSearchContainersFixture _fixture;

    public CatalogSearchFilterCombinationContainersTests(MarketplaceCatalogSearchContainersFixture fixture) => _fixture = fixture;

    public static IEnumerable<object[]> AllContainerCombinations()
        => CatalogSearchFilterCombinationGenerator.AllContainerCombinations();

    [Theory]
    [MemberData(nameof(AllContainerCombinations))]
    public async Task Db_Fallback_Combines_All_Filter_Dimensions_With_And_Semantics(
        byte mask,
        SearchCatalogProductsQuery query,
        string[] expectedSlugs)
    {
        var serviceProvider = _fixture.CreateServiceProvider();

        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = new SearchCatalogProductsQueryHandler(
            new EmptyElasticsearchSearchService(),
            scope.ServiceProvider.GetRequiredService<IProductRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductDetailRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductImageRepository>(),
            scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>(),
            scope.ServiceProvider.GetRequiredService<ICategoryRepository>(),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess, $"mask={mask}: {result.Error}");

        var actualSlugs = result.Value!.Items
            .Select(item => item.Slug)
            .OrderBy(slug => slug, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            expectedSlugs.Length == result.Value.Total && expectedSlugs.SequenceEqual(actualSlugs),
            $"mask={mask}: expected [{string.Join(", ", expectedSlugs)}], actual [{string.Join(", ", actualSlugs)}], total={result.Value.Total}");
    }

    [Theory]
    [MemberData(nameof(AllContainerCombinations))]
    public async Task Elasticsearch_Combines_All_Filter_Dimensions_With_And_Semantics(
        byte mask,
        SearchCatalogProductsQuery query,
        string[] expectedSlugs)
    {
        var serviceProvider = _fixture.CreateServiceProvider();

        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = new SearchCatalogProductsQueryHandler(
            scope.ServiceProvider.GetRequiredService<IProductSearchService>(),
            scope.ServiceProvider.GetRequiredService<IProductRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductDetailRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductImageRepository>(),
            scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>(),
            scope.ServiceProvider.GetRequiredService<ICategoryRepository>(),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess, $"mask={mask}: {result.Error}");

        var actualSlugs = result.Value!.Items
            .Select(item => item.Slug)
            .OrderBy(slug => slug, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            expectedSlugs.Length == result.Value.Total && expectedSlugs.SequenceEqual(actualSlugs),
            $"mask={mask}: expected [{string.Join(", ", expectedSlugs)}], actual [{string.Join(", ", actualSlugs)}], total={result.Value.Total}");
    }

    [Fact]
    public async Task Elasticsearch_Combines_Parent_Category_And_Structural_Filters()
    {
        var serviceProvider = _fixture.CreateServiceProvider();

        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = new SearchCatalogProductsQueryHandler(
            scope.ServiceProvider.GetRequiredService<IProductSearchService>(),
            scope.ServiceProvider.GetRequiredService<IProductRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductDetailRepository>(),
            scope.ServiceProvider.GetRequiredService<IProductImageRepository>(),
            scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>(),
            scope.ServiceProvider.GetRequiredService<ICategoryRepository>(),
            NullLogger<SearchCatalogProductsQueryHandler>.Instance);

        var result = await handler.Handle(
            CatalogSearchFilterOracle.BuildQuery(
                CatalogSearchFilterOracle.ContainerFilterValues,
                (byte)(CatalogSearchFilterDimension.Category | CatalogSearchFilterDimension.Price | CatalogSearchFilterDimension.Author)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(["hobbit-fantasy"], result.Value!.Items.Select(item => item.Slug).ToArray());
    }

    private sealed class EmptyElasticsearchSearchService : IProductSearchService
    {
        private static readonly Result<ProductSearchResultDto> EmptyResult =
            Result<ProductSearchResultDto>.Success(new ProductSearchResultDto([], 0, 1, 200));

        public Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
            CatalogProductSearchFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(EmptyResult);

        public Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(
            CatalogOnSaleProductFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(EmptyResult);

        public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(EmptyResult);

        public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(
            CatalogBrowsableProductFilters filters,
            CancellationToken ct = default)
            => Task.FromResult(EmptyResult);
    }
}
