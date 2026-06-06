using Marketplace.Application.Carts.Commands.AddCartItem;
using Marketplace.Application.Carts.Commands.ClearCart;
using Marketplace.Application.Carts.Commands.RemoveCartItem;
using Marketplace.Application.Carts.Commands.UpdateCartItemQuantity;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Carts.Services;
using Marketplace.Application.Carts.Queries.GetMyCart;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "CartCheckout")]
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
        var watchRepo = new NoopCartStockWatchRepository();
        var stockRepo = new CartTestWarehouseStockRepository();
        var sync = new CartStockWatchSyncService(itemRepo, watchRepo, productRepo, stockRepo);
        var handler = new AddCartItemCommandHandler(cartRepo, itemRepo, productRepo, cache, sync);

        var first = await handler.Handle(new AddCartItemCommand(userId, 1001, 2), CancellationToken.None);
        var second = await handler.Handle(new AddCartItemCommand(userId, 1001, 3), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(second.Value!.Items);
        Assert.Equal(5, second.Value.Items[0].Quantity);
        Assert.Contains($"cart:user:{userId}:active", cache.RemovedKeys);
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

        var productRepo = new InMemoryProductRepository();
        productRepo.Seed(CreateActiveProduct(5));
        var watchRepo = new NoopCartStockWatchRepository();
        var stockRepo = new CartTestWarehouseStockRepository();
        var sync = new CartStockWatchSyncService(itemRepo, watchRepo, productRepo, stockRepo);
        var handler = new RemoveCartItemCommandHandler(cartRepo, itemRepo, new NoOpCachePort(), sync);
        var result = await handler.Handle(new RemoveCartItemCommand(secondUser, item.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("cart not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddCartItem_Fails_For_NonPositive_Quantity()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var productRepo = new InMemoryProductRepository();
        productRepo.Seed(CreateActiveProduct(101));
        var sync = new CartStockWatchSyncService(itemRepo, new NoopCartStockWatchRepository(), productRepo, new CartTestWarehouseStockRepository());
        var handler = new AddCartItemCommandHandler(cartRepo, itemRepo, productRepo, new NoOpCachePort(), sync);

        var result = await handler.Handle(new AddCartItemCommand(Guid.NewGuid(), 101, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("greater than zero", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCartItemQuantity_Fails_For_NonPositive_Quantity()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null), CancellationToken.None);
        var item = await itemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 1, new Money(50), Money.Zero, now, now, false, null), CancellationToken.None);

        var handler = new UpdateCartItemQuantityCommandHandler(
            cartRepo,
            itemRepo,
            new NoOpCachePort(),
            new CartStockWatchSyncService(itemRepo, new NoopCartStockWatchRepository(), new InMemoryProductRepository(), new CartTestWarehouseStockRepository()));

        var result = await handler.Handle(new UpdateCartItemQuantityCommand(userId, item.Id.Value, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("greater than zero", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCartItemQuantity_Fails_When_Item_Belongs_To_Other_User()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), firstUser, CartStatus.Active, now, now, now, false, null), CancellationToken.None);
        var item = await itemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 1, new Money(10), Money.Zero, now, now, false, null), CancellationToken.None);
        var handler = new UpdateCartItemQuantityCommandHandler(
            cartRepo,
            itemRepo,
            new NoOpCachePort(),
            new CartStockWatchSyncService(itemRepo, new NoopCartStockWatchRepository(), new InMemoryProductRepository(), new CartTestWarehouseStockRepository()));

        var result = await handler.Handle(new UpdateCartItemQuantityCommand(secondUser, item.Id.Value, 3), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("cart not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClearCart_Twice_Remains_Idempotent_And_Empty()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null), CancellationToken.None);
        _ = await itemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 2, new Money(10), Money.Zero, now, now, false, null), CancellationToken.None);

        var handler = new ClearCartCommandHandler(cartRepo, itemRepo, new NoOpCachePort(), new NoopCartStockWatchRepository());

        var first = await handler.Handle(new ClearCartCommand(userId), CancellationToken.None);
        var second = await handler.Handle(new ClearCartCommand(userId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Empty(first.Value!.Items);
        Assert.Empty(second.Value!.Items);
    }

    [Fact]
    public async Task AddAndRemoveItem_Triggers_StockWatchSync()
    {
        var cartRepo = new InMemoryCartRepository();
        var itemRepo = new InMemoryCartItemRepository();
        var productRepo = new InMemoryProductRepository();
        productRepo.Seed(CreateActiveProduct(305));
        var watchRepo = new SpyCartStockWatchRepository();
        var sync = new CartStockWatchSyncService(itemRepo, watchRepo, productRepo, new LowStockWarehouseStockRepository());
        var addHandler = new AddCartItemCommandHandler(cartRepo, itemRepo, productRepo, new NoOpCachePort(), sync);
        var removeHandler = new RemoveCartItemCommandHandler(cartRepo, itemRepo, new NoOpCachePort(), sync);
        var userId = Guid.NewGuid();

        var add = await addHandler.Handle(new AddCartItemCommand(userId, 305, 1), CancellationToken.None);
        Assert.True(add.IsSuccess);
        var itemId = add.Value!.Items.Single().Id;

        var remove = await removeHandler.Handle(new RemoveCartItemCommand(userId, itemId), CancellationToken.None);

        Assert.True(remove.IsSuccess);
        Assert.Contains((userId, 305L), watchRepo.Upserts);
        Assert.Contains((userId, 305L), watchRepo.Deletes);
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

    private sealed class NoopCartStockWatchRepository : ICartStockWatchRepository
    {
        public Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(
            long productId, TimeSpan minIntervalSinceLastNotify, DateTime utcNow, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class SpyCartStockWatchRepository : ICartStockWatchRepository
    {
        public List<(Guid UserId, long ProductId)> Upserts { get; } = [];
        public List<(Guid UserId, long ProductId)> Deletes { get; } = [];

        public Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default)
        {
            Upserts.Add((userId, productId));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default)
        {
            Deletes.Add((userId, productId));
            return Task.CompletedTask;
        }

        public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(long productId, TimeSpan minIntervalSinceLastNotify, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CartTestWarehouseStockRepository : IWarehouseStockRepository
    {
        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult<WarehouseStock?>(null);

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>([]);

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>([
                WarehouseStock.Reconstitute(
                    WarehouseStockId.From(1),
                    companyId,
                    WarehouseId.From(1),
                    productId,
                    1000,
                    0,
                    0,
                    1,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null)
            ]);

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class LowStockWarehouseStockRepository : IWarehouseStockRepository
    {
        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult<WarehouseStock?>(null);

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>([]);

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>([
                WarehouseStock.Reconstitute(
                    WarehouseStockId.From(1),
                    companyId,
                    WarehouseId.From(1),
                    productId,
                    0,
                    0,
                    0,
                    1,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null)
            ]);

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }
}
