using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Carts.Services;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using System.Linq;

namespace Marketplace.Tests;

[Trait("Suite", "Inventory")]
[Trait("Suite", "Notifications")]
public sealed class ApplicationRestockNotificationsTests
{
    [Fact]
    public async Task RestockNotifier_Schedules_When_Available_Crosses_From_Zero()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var watchRepo = new SpyCartStockWatchRepository { EligibleUserIds = [userId] };
        var productRepo = new SingleProductRepository(CreateProduct(1, companyId));
        var spyScheduler = new SpyAppNotificationScheduler();
        var notifier = new RestockAvailabilityNotifier(watchRepo, productRepo, spyScheduler);

        await notifier.NotifyIfCrossedFromZeroAsync(companyId, 1, beforeAvailableSum: 0, afterAvailableSum: 3, CancellationToken.None);

        Assert.Single(spyScheduler.Requests);
        Assert.Equal(AppNotificationTemplateKeys.CartProductBackInStock, spyScheduler.Requests[0].TemplateKey);
        Assert.Equal(userId, spyScheduler.Requests[0].TargetUserId);
        Assert.Contains((userId, 1L), watchRepo.TouchCalls);
    }

    [Fact]
    public async Task RestockNotifier_Does_Not_Schedule_When_Already_In_Stock()
    {
        var companyId = Guid.NewGuid();
        var watchRepo = new SpyCartStockWatchRepository { EligibleUserIds = [Guid.NewGuid()] };
        var productRepo = new SingleProductRepository(CreateProduct(2, companyId));
        var spyScheduler = new SpyAppNotificationScheduler();
        var notifier = new RestockAvailabilityNotifier(watchRepo, productRepo, spyScheduler);

        await notifier.NotifyIfCrossedFromZeroAsync(companyId, 2, beforeAvailableSum: 2, afterAvailableSum: 5, CancellationToken.None);

        Assert.Empty(spyScheduler.Requests);
    }

    [Fact]
    public async Task CartStockWatchSync_Upserts_When_CartQty_Exceeds_Available()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cart = Cart.Reconstitute(CartId.From(1), userId, CartStatus.Active, now, now, now, false, null);
        var itemRepo = new LocalCartItemRepository();
        await itemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(1), cart.Id, ProductId.From(9), 3, new Money(10), Money.Zero, now, now, false, null), CancellationToken.None);

        var companyId = Guid.NewGuid();
        var companyVo = CompanyId.From(companyId);
        var productRepo = new SingleProductRepository(CreateProduct(9, companyId));
        var watchSpy = new SpyCartStockWatchRepository();
        var stockRepo = new ZeroAvailableStockRepository(companyVo, ProductId.From(9));
        var sync = new CartStockWatchSyncService(itemRepo, watchSpy, productRepo, stockRepo);

        await sync.SyncWatchForUserCartProductAsync(userId, cart.Id, ProductId.From(9), CancellationToken.None);

        Assert.Equal(1, watchSpy.UpsertCalls);
    }

    private static Product CreateProduct(long id, Guid companyGuid)
    {
        var now = DateTime.UtcNow;
        return Product.Reconstitute(
            ProductId.From(id),
            CompanyId.From(companyGuid),
            "P",
            $"p-{id}",
            "d",
            new Money(10),
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
            now,
            now,
            false,
            null);
    }

    private sealed class SpyAppNotificationScheduler : IAppNotificationScheduler
    {
        public List<AppNotificationRequest> Requests { get; } = [];

        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class SpyCartStockWatchRepository : ICartStockWatchRepository
    {
        public IReadOnlyList<Guid> EligibleUserIds { get; init; } = [];
        public int UpsertCalls { get; private set; }
        public List<(Guid userId, long productId)> TouchCalls { get; } = [];

        public Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default)
        {
            UpsertCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(
            long productId, TimeSpan minIntervalSinceLastNotify, DateTime utcNow, CancellationToken ct = default) =>
            Task.FromResult(EligibleUserIds);

        public Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default)
        {
            TouchCalls.Add((userId, productId));
            return Task.CompletedTask;
        }
    }

    private sealed class SingleProductRepository : IProductRepository
    {
        private readonly Product _product;

        public SingleProductRepository(Product product) => _product = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) =>
            Task.FromResult(id.Value == _product.Id.Value ? _product : null);

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default) =>
            Task.FromResult<Product?>(null);

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult<Product?>(null);

        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(ids.Any(x => x.Value == _product.Id.Value) ? [_product] : []);

        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>([_product]);

        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(
                _product.Status == ProductStatus.PendingReview ? [_product] : []);

        public Task AddAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ZeroAvailableStockRepository : IWarehouseStockRepository
    {
        private readonly CompanyId _companyId;
        private readonly ProductId _productId;

        public ZeroAvailableStockRepository(CompanyId companyId, ProductId productId)
        {
            _companyId = companyId;
            _productId = productId;
        }

        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult<WarehouseStock?>(null);

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>([]);

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
        {
            if (companyId != _companyId || productId != _productId)
                return Task.FromResult<IReadOnlyList<WarehouseStock>>([]);

            var row = WarehouseStock.Reconstitute(
                WarehouseStockId.From(1),
                companyId,
                WarehouseId.From(1),
                productId,
                onHand: 0,
                reserved: 0,
                reorderPoint: 0,
                version: 1,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null);
            return Task.FromResult<IReadOnlyList<WarehouseStock>>([row]);
        }

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class LocalCartItemRepository : ICartItemRepository
    {
        private readonly Dictionary<long, CartItem> _items = [];

        public Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default) =>
            Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(x => x.CartId == cartId && x.ProductId == productId && !x.IsDeleted));

        public Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CartItem>>(_items.Values.Where(x => x.CartId == cartId && !x.IsDeleted).ToList());

        public Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default)
        {
            _items[item.Id.Value] = item;
            return Task.FromResult(item);
        }

        public Task UpdateAsync(CartItem item, CancellationToken ct = default)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(CartItemId id, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
        public Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
    }
}
