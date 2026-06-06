using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Entities;
using Marketplace.Domain.Favorites.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Favorites")]
public class ApplicationFavoriteHandlersTests
{
    [Fact]
    public async Task AddFavorite_Twice_Is_Idempotent()
    {
        var favorites = new InMemoryFavoriteRepository();
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        products.Seed(CreateActiveProduct(3001));
        var userId = Guid.NewGuid();
        var sut = new AddFavoriteProductCommandHandler(favorites, products, cache);

        var first = await sut.Handle(new AddFavoriteProductCommand(userId, 3001), CancellationToken.None);
        var second = await sut.Handle(new AddFavoriteProductCommand(userId, 3001), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(1, favorites.ActiveCount(userId, 3001));
    }

    [Fact]
    public async Task AddFavorite_After_Remove_Reactivates_Row()
    {
        var favorites = new InMemoryFavoriteRepository();
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        products.Seed(CreateActiveProduct(3002));
        var userId = Guid.NewGuid();
        var add = new AddFavoriteProductCommandHandler(favorites, products, cache);
        var remove = new RemoveFavoriteProductCommandHandler(favorites, cache);

        var created = await add.Handle(new AddFavoriteProductCommand(userId, 3002), CancellationToken.None);
        _ = await remove.Handle(new RemoveFavoriteProductCommand(userId, 3002), CancellationToken.None);
        var restored = await add.Handle(new AddFavoriteProductCommand(userId, 3002), CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.True(restored.IsSuccess);
        Assert.Equal(created.Value!.Id, restored.Value!.Id);
        Assert.Equal(1, favorites.AllCount(userId, 3002));
        Assert.Equal(1, favorites.ActiveCount(userId, 3002));
    }

    [Fact]
    public async Task RemoveFavorite_Is_Idempotent()
    {
        var favorites = new InMemoryFavoriteRepository();
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        products.Seed(CreateActiveProduct(3003));
        var userId = Guid.NewGuid();
        var add = new AddFavoriteProductCommandHandler(favorites, products, cache);
        var remove = new RemoveFavoriteProductCommandHandler(favorites, cache);

        var added = await add.Handle(new AddFavoriteProductCommand(userId, 3003), CancellationToken.None);
        var firstRemove = await remove.Handle(new RemoveFavoriteProductCommand(userId, 3003), CancellationToken.None);
        var secondRemove = await remove.Handle(new RemoveFavoriteProductCommand(userId, 3003), CancellationToken.None);

        Assert.True(added.IsSuccess);
        Assert.True(firstRemove.IsSuccess);
        Assert.True(secondRemove.IsSuccess);
        Assert.True(firstRemove.Value);
        Assert.True(secondRemove.Value);
    }

    [Fact]
    public async Task AddFavorite_Fails_For_Inactive_Product()
    {
        var favorites = new InMemoryFavoriteRepository();
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        products.Seed(CreateProduct(3010, ProductStatus.PendingReview, false));
        var sut = new AddFavoriteProductCommandHandler(favorites, products, cache);

        var result = await sut.Handle(new AddFavoriteProductCommand(Guid.NewGuid(), 3010), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMyFavorites_Uses_Cache_On_Second_Call()
    {
        var favorites = new InMemoryFavoriteRepository();
        var cache = new SpyCachePort();
        var ttl = Options.Create(new CacheTtlOptions());
        var userId = Guid.NewGuid();
        await favorites.AddAsync(
            Favorite.Create(FavoriteId.From(0), userId, ProductId.From(4001), DateTime.UtcNow, new Money(10m)),
            CancellationToken.None);
        var sut = new GetMyFavoritesQueryHandler(favorites, cache, ttl);

        var first = await sut.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);
        var second = await sut.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, cache.GetHitsFor(FavoritesCacheKeys.ListByUser(userId)));
    }

    [Fact]
    public async Task Favorites_Mutations_Invalidate_User_Cache_Key()
    {
        var favorites = new InMemoryFavoriteRepository();
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        products.Seed(CreateActiveProduct(5001));
        var userId = Guid.NewGuid();
        var add = new AddFavoriteProductCommandHandler(favorites, products, cache);
        var remove = new RemoveFavoriteProductCommandHandler(favorites, cache);

        _ = await add.Handle(new AddFavoriteProductCommand(userId, 5001), CancellationToken.None);
        _ = await remove.Handle(new RemoveFavoriteProductCommand(userId, 5001), CancellationToken.None);

        Assert.Contains(FavoritesCacheKeys.ListByUser(userId), cache.RemovedKeys);
        Assert.True(cache.RemovedKeys.Count(x => x == FavoritesCacheKeys.ListByUser(userId)) >= 2);
    }

    private static Product CreateActiveProduct(long id) => CreateProduct(id, ProductStatus.Active, false);

    private static Product CreateProduct(long id, ProductStatus status, bool isDeleted)
    {
        return Product.Reconstitute(
            ProductId.From(id),
            CompanyId.From(Guid.NewGuid()),
            $"Product {id}",
            $"product-{id}",
            "Desc",
            new Money(99m),
            null,
            5,
            1,
            CategoryId.From(1),
            status,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            isDeleted,
            isDeleted ? DateTime.UtcNow : null);
    }

    private sealed class InMemoryFavoriteRepository : IFavoriteRepository
    {
        private readonly Dictionary<long, Favorite> _items = new();
        private long _nextId = 1;

        public int ActiveCount(Guid userId, long productId)
            => _items.Values.Count(x => x.UserId == userId && x.ProductId.Value == productId && !x.IsDeleted);

        public int AllCount(Guid userId, long productId)
            => _items.Values.Count(x => x.UserId == userId && x.ProductId.Value == productId);

        public Task<Favorite?> GetByUserAndProductAsync(Guid userId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.UserId == userId && x.ProductId == productId && !x.IsDeleted));

        public Task<Favorite?> GetByUserAndProductIncludingDeletedAsync(Guid userId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.UserId == userId && x.ProductId == productId));

        public Task<IReadOnlyList<Favorite>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Favorite>>(_items.Values.Where(x => x.UserId == userId && !x.IsDeleted).OrderByDescending(x => x.AddedAt).ToList());

        public Task<Favorite> AddAsync(Favorite favorite, CancellationToken ct = default)
        {
            var existingActive = _items.Values.FirstOrDefault(x => x.UserId == favorite.UserId && x.ProductId == favorite.ProductId && !x.IsDeleted);
            if (existingActive is not null)
                throw new InvalidOperationException("Duplicate active favorite");

            var id = favorite.Id.Value <= 0 ? _nextId++ : favorite.Id.Value;
            var stored = Favorite.Reconstitute(
                FavoriteId.From(id),
                favorite.UserId,
                favorite.ProductId,
                favorite.AddedAt,
                favorite.PriceAtAdd,
                favorite.IsAvailable,
                favorite.Notifications,
                favorite.Meta,
                favorite.CreatedAt,
                favorite.UpdatedAt,
                favorite.IsDeleted,
                favorite.DeletedAt);
            _items[id] = stored;
            return Task.FromResult(stored);
        }

        public Task ReactivateAsync(FavoriteId id, DateTime utcNow, Money? priceAtAdd, CancellationToken ct = default)
        {
            if (!_items.TryGetValue(id.Value, out var favorite))
                return Task.CompletedTask;

            _items[id.Value] = Favorite.Reconstitute(
                favorite.Id,
                favorite.UserId,
                favorite.ProductId,
                utcNow,
                priceAtAdd,
                true,
                favorite.Notifications,
                favorite.Meta,
                favorite.CreatedAt,
                utcNow,
                false,
                null);

            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(FavoriteId id, DateTime utcNow, CancellationToken ct = default)
        {
            if (!_items.TryGetValue(id.Value, out var favorite) || favorite.IsDeleted)
                return Task.CompletedTask;

            _items[id.Value] = Favorite.Reconstitute(
                favorite.Id,
                favorite.UserId,
                favorite.ProductId,
                favorite.AddedAt,
                favorite.PriceAtAdd,
                favorite.IsAvailable,
                favorite.Notifications,
                favorite.Meta,
                favorite.CreatedAt,
                utcNow,
                true,
                utcNow);

            return Task.CompletedTask;
        }
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

    private sealed class SpyCachePort : IAppCachePort
    {
        private readonly Dictionary<string, object> _values = new();
        private readonly Dictionary<string, int> _getCalls = new();

        public List<string> RemovedKeys { get; } = [];

        public int GetHitsFor(string key) => _getCalls.TryGetValue(key, out var count) ? Math.Max(0, count - 1) : 0;

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            _getCalls[key] = _getCalls.TryGetValue(key, out var count) ? count + 1 : 1;
            if (_values.TryGetValue(key, out var value) && value is T typed)
                return Task.FromResult<T?>(typed);

            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
