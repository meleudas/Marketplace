using Marketplace.Application.Carts.Commands.AddCartItem;
using Marketplace.Application.Carts.Commands.RemoveCartItem;
using Marketplace.Application.Carts.Queries.GetMyCart;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Entities;
using Marketplace.Domain.Favorites.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

public class ApplicationCartFavoriteHandlersTests
{
    [Fact]
    public async Task GetMyCart_Creates_Empty_ActiveCart_When_Missing()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var handler = new GetMyCartQueryHandler(cartRepo, itemRepo, new NoOpCachePort(), Options.Create(new CacheTtlOptions()));

        var userId = Guid.NewGuid();
        var result = await handler.Handle(new GetMyCartQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value!.UserId);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task AddCartItem_Twice_Increments_Quantity()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var productRepo = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        productRepo.Seed(CreateActiveProduct(1001));

        var userId = Guid.NewGuid();
        var handler = new AddCartItemCommandHandler(cartRepo, itemRepo, productRepo, cache);

        var first = await handler.Handle(new AddCartItemCommand(userId, 1001, 2), CancellationToken.None);
        var second = await handler.Handle(new AddCartItemCommand(userId, 1001, 3), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(second.Value!.Items);
        Assert.Equal(5, second.Value.Items[0].Quantity);
        Assert.Contains($"cart:user:{userId}:active", cache.RemovedKeys);
    }

    [Fact]
    public async Task RemoveFavorite_Is_Idempotent()
    {
        var favoriteRepo = new InMemoryFavoriteRepository();
        var productRepo = new InMemoryProductRepository();
        productRepo.Seed(CreateActiveProduct(2002));

        var userId = Guid.NewGuid();
        var addHandler = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, new NoOpCachePort());
        var removeHandler = new RemoveFavoriteProductCommandHandler(favoriteRepo, new NoOpCachePort());

        var added = await addHandler.Handle(new AddFavoriteProductCommand(userId, 2002), CancellationToken.None);
        var firstRemove = await removeHandler.Handle(new RemoveFavoriteProductCommand(userId, 2002), CancellationToken.None);
        var secondRemove = await removeHandler.Handle(new RemoveFavoriteProductCommand(userId, 2002), CancellationToken.None);

        Assert.True(added.IsSuccess);
        Assert.True(firstRemove.IsSuccess);
        Assert.True(secondRemove.IsSuccess);
        Assert.True(firstRemove.Value);
        Assert.True(secondRemove.Value);
    }

    [Fact]
    public async Task GetMyCart_Returns_Cached_Value_When_Present()
    {
        var userId = Guid.NewGuid();
        var cache = new SpyCachePort();
        var cachedDto = new Marketplace.Application.Carts.DTOs.CartDto(88, userId, DateTime.UtcNow, [], 0, 0);
        cache.Cached["cart:user:" + userId + ":active"] = cachedDto;

        var handler = new GetMyCartQueryHandler(new InMemoryCartRepository(), new InMemoryCartItemRepository(), cache, Options.Create(new CacheTtlOptions()));
        var result = await handler.Handle(new GetMyCartQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(88, result.Value!.Id);
    }

    [Fact]
    public async Task RemoveCartItem_Fails_When_Item_Belongs_To_Other_User()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cart = await cartRepo.AddAsync(
            Cart.Reconstitute(CartId.From(0), firstUser, CartStatus.Active, now, now, now, false, null),
            CancellationToken.None);
        var item = await itemRepo.AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(5), 1, new Money(10), Money.Zero, now, now, false, null),
            CancellationToken.None);

        var handler = new RemoveCartItemCommandHandler(cartRepo, itemRepo, new NoOpCachePort());
        var result = await handler.Handle(new RemoveCartItemCommand(secondUser, item.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("cart not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static Product CreateActiveProduct(long id)
    {
        return Product.Reconstitute(
            ProductId.From(id),
            CompanyId.From(Guid.NewGuid()),
            "Test product",
            $"test-product-{id}",
            "Description",
            new Money(199),
            null,
            5,
            1,
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
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly Dictionary<long, Cart> _items = new();
        private long _nextId = 1;

        public Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.UserId == userId && x.Status == CartStatus.Active && !x.IsDeleted));

        public Task<Cart?> GetByIdAsync(CartId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<Cart> AddAsync(Cart cart, CancellationToken ct = default)
        {
            var id = cart.Id.Value <= 0 ? _nextId++ : cart.Id.Value;
            var stored = Cart.Reconstitute(
                CartId.From(id),
                cart.UserId,
                cart.Status,
                cart.LastActivityAt,
                cart.CreatedAt,
                cart.UpdatedAt,
                cart.IsDeleted,
                cart.DeletedAt);
            _items[id] = stored;
            return Task.FromResult(stored);
        }

        public Task UpdateAsync(Cart cart, CancellationToken ct = default)
        {
            _items[cart.Id.Value] = cart;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCartItemRepository : ICartItemRepository
    {
        private readonly Dictionary<long, CartItem> _items = new();
        private long _nextId = 1;

        public Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.CartId == cartId && x.ProductId == productId && !x.IsDeleted));

        public Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CartItem>>(_items.Values.Where(x => x.CartId == cartId && !x.IsDeleted).ToList());

        public Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default)
        {
            var id = item.Id.Value <= 0 ? _nextId++ : item.Id.Value;
            var stored = CartItem.Reconstitute(
                CartItemId.From(id),
                item.CartId,
                item.ProductId,
                item.Quantity,
                item.PriceAtMoment,
                item.Discount,
                item.CreatedAt,
                item.UpdatedAt,
                item.IsDeleted,
                item.DeletedAt);
            _items[id] = stored;
            return Task.FromResult(stored);
        }

        public Task UpdateAsync(CartItem item, CancellationToken ct = default)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(CartItemId id, DateTime utcNow, CancellationToken ct = default)
        {
            if (!_items.TryGetValue(id.Value, out var item) || item.IsDeleted)
                return Task.CompletedTask;

            _items[id.Value] = CartItem.Reconstitute(
                item.Id,
                item.CartId,
                item.ProductId,
                item.Quantity,
                item.PriceAtMoment,
                item.Discount,
                item.CreatedAt,
                utcNow,
                true,
                utcNow);
            return Task.CompletedTask;
        }

        public Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default)
        {
            foreach (var item in _items.Values.Where(x => x.CartId == cartId && !x.IsDeleted).ToList())
            {
                _items[item.Id.Value] = CartItem.Reconstitute(
                    item.Id,
                    item.CartId,
                    item.ProductId,
                    item.Quantity,
                    item.PriceAtMoment,
                    item.Discount,
                    item.CreatedAt,
                    utcNow,
                    true,
                    utcNow);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFavoriteRepository : IFavoriteRepository
    {
        private readonly Dictionary<long, Favorite> _items = new();
        private long _nextId = 1;

        public Task<Favorite?> GetByUserAndProductAsync(Guid userId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.UserId == userId && x.ProductId == productId && !x.IsDeleted));

        public Task<IReadOnlyList<Favorite>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Favorite>>(_items.Values.Where(x => x.UserId == userId && !x.IsDeleted).ToList());

        public Task<Favorite> AddAsync(Favorite favorite, CancellationToken ct = default)
        {
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

    private sealed class NoOpCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public Dictionary<string, object> Cached { get; } = new();
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            if (Cached.TryGetValue(key, out var value) && value is T typed)
                return Task.FromResult<T?>(typed);

            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            Cached[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            Cached.Remove(key);
            return Task.CompletedTask;
        }
    }
}
