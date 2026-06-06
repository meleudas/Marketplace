using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Categories.Queries.GetAllCategories;
using Marketplace.Application.Categories.Queries.GetCatalogCategoryById;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class ApplicationCatalogCategoriesQueryTests
{
    [Fact]
    public async Task GetActiveCategories_Uses_Cache_On_Second_Call()
    {
        var repo = new InMemoryCategoryRepository();
        repo.Items[1] = Category.Create(CategoryId.From(1), "A", "a", null, null, null, JsonBlob.Empty, 0, true);
        var cache = new SpyCachePort();
        var handler = new GetActiveCategoriesQueryHandler(repo, cache, Options.Create(new CacheTtlOptions()));

        var first = await handler.Handle(new GetActiveCategoriesQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetActiveCategoriesQuery(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, repo.GetActiveCalls);
        Assert.Contains(cache.SetCalls, x => x.Key == CatalogCacheKeys.ActiveCategories);
    }

    [Fact]
    public async Task GetCatalogCategoryById_Returns_NotFound_For_Inactive_Category()
    {
        var repo = new InMemoryCategoryRepository();
        repo.Items[2] = Category.Create(CategoryId.From(2), "B", "b", null, null, null, JsonBlob.Empty, 0, false);
        var handler = new GetCatalogCategoryByIdQueryHandler(repo, new SpyCachePort(), Options.Create(new CacheTtlOptions()));

        var result = await handler.Handle(new GetCatalogCategoryByIdQuery(2), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllCategories_Uses_AdminCache()
    {
        var repo = new InMemoryCategoryRepository();
        repo.Items[10] = Category.Create(CategoryId.From(10), "C", "c", null, null, null, JsonBlob.Empty, 0, true);
        var cache = new SpyCachePort();
        var handler = new GetAllCategoriesQueryHandler(repo, cache, Options.Create(new CacheTtlOptions()));

        _ = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);
        _ = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

        Assert.Equal(1, repo.GetAllCalls);
        Assert.Contains(cache.SetCalls, x => x.Key == CatalogCacheKeys.AllCategories);
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        public Dictionary<long, Category> Items { get; } = new();
        public int GetActiveCalls { get; private set; }
        public int GetAllCalls { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
            => Task.FromResult(Items.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        {
            GetAllCalls++;
            return Task.FromResult<IReadOnlyList<Category>>(Items.Values.Where(x => !x.IsDeleted).ToList());
        }

        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default)
        {
            GetActiveCalls++;
            return Task.FromResult<IReadOnlyList<Category>>(Items.Values.Where(x => x.IsActive && !x.IsDeleted).ToList());
        }

        public Task<Category> AddAsync(Category category, CancellationToken ct = default)
        {
            Items[category.Id.Value] = category;
            return Task.FromResult(category);
        }

        public Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            Items[category.Id.Value] = category;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        private readonly Dictionary<string, object> _cache = new();
        public List<(string Key, object Value)> SetCalls { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_cache.TryGetValue(key, out var value) && value is T typed ? typed : null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _cache[key] = value;
            SetCalls.Add((key, value));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
