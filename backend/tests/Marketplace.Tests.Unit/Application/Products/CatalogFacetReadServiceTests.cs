using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.GetCatalogAuthors;
using Marketplace.Application.Products.Queries.GetCatalogProductFacets;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogFacets")]
public class CatalogFacetReadServiceTests
{
    [Fact]
    public async Task GetCatalogProductFacetsQueryHandler_Uses_Cache_On_Second_Call()
    {
        var repo = new StubFacetSourceRepository();
        var cache = new SpyCachePort();
        var service = new CatalogFacetReadService(
            repo,
            new CatalogFacetAggregator(),
            cache,
            Options.Create(new CacheTtlOptions()));
        var handler = new GetCatalogProductFacetsQueryHandler(service, new EmptyCategoryRepository());

        var first = await handler.Handle(new GetCatalogProductFacetsQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetCatalogProductFacetsQuery(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, repo.CallCount);
        Assert.Contains(cache.Keys, x => x.StartsWith(CatalogCacheKeys.ProductFacetsPrefix, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCatalogAuthorsQueryHandler_Returns_Only_Authors()
    {
        var repo = new StubFacetSourceRepository();
        var service = new CatalogFacetReadService(
            repo,
            new CatalogFacetAggregator(),
            new SpyCachePort(),
            Options.Create(new CacheTtlOptions()));
        var handler = new GetCatalogAuthorsQueryHandler(service, new EmptyCategoryRepository());

        var result = await handler.Handle(new GetCatalogAuthorsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("tolkien", result.Value![0].Value);
        Assert.DoesNotContain(result.Value, x => x.Value == "fantasy");
    }

    private sealed class EmptyCategoryRepository : ICategoryRepository
    {
        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default) => Task.FromResult<Category?>(null);
        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<Category> AddAsync(Category category, CancellationToken ct = default) => Task.FromResult(category);
        public Task UpdateAsync(Category category, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubFacetSourceRepository : IProductFacetSourceRepository
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<ProductFacetSourceRow>> ListActiveFacetSourcesAsync(
            IReadOnlyList<long>? categoryIds = null,
            Guid? companyId = null,
            CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<ProductFacetSourceRow>>(
            [
                new(1, """{"author":"Tolkien","format":"паперова","genre":"fantasy"}""", ["bestseller"], [])
            ]);
        }
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public List<string> Keys { get; } = [];
        private readonly Dictionary<string, object> _items = new(StringComparer.Ordinal);

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            if (_items.TryGetValue(key, out var value) && value is T typed)
                return Task.FromResult<T?>(typed);

            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            Keys.Add(key);
            _items[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _items.Remove(key);
            return Task.CompletedTask;
        }
    }
}
