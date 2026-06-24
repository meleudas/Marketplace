using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "CartCheckout")]
public class ApplicationCheckoutCartCommandTests
{
    [Fact]
    public async Task Checkout_Splits_Cart_By_Company_And_Creates_Pending_Orders()
    {
        var userId = Guid.NewGuid();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cartRepo = new InMemoryCartRepository();
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null));
        var cartItemRepo = new InMemoryCartItemRepository();
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 2, new Money(100), Money.Zero, now, now, false, null));
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(2), 1, new Money(80), Money.Zero, now, now, false, null));
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(3), 1, new Money(50), Money.Zero, now, now, false, null));

        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(ProductId.From(1), CompanyId.From(companyA), "A1", "a1", "d", new Money(100), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));
        products.Seed(Product.Reconstitute(ProductId.From(2), CompanyId.From(companyA), "A2", "a2", "d", new Money(80), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));
        products.Seed(Product.Reconstitute(ProductId.From(3), CompanyId.From(companyB), "B1", "b1", "d", new Money(50), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));

        var orderRepo = new InMemoryOrderRepository();
        var orderItemRepo = new InMemoryOrderItemRepository();
        var orderAddressRepo = new InMemoryOrderAddressRepository();
        var cache = new SpyCachePort();

        var handler = new CheckoutCartCommandHandler(
            cartRepo,
            cartItemRepo,
            products,
            orderRepo,
            orderItemRepo,
            orderAddressRepo,
            new InMemoryPaymentRepository(),
            new FakeLiqPayPort(),
            cache,
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new InMemoryWarehouseStockRepository(),
            new WarehouseAllocationPlanner(new InMemoryWarehouseRepository(), new InMemoryWarehouseStockRepository()),
            new NoopAppNotificationScheduler(),
            new NoopCartStockWatchRepository(),
            new FakeShippingMethodRepository(),
            new NoopCouponCheckoutService(),
            new NoopAppTransactionPort(),
            NullLogger<CheckoutCartCommandHandler>.Instance);
        var cmd = new CheckoutCartCommand(
            userId,
            CheckoutPaymentMethod.Card,
            1,
            new CheckoutAddressDto("Ім'я", "Прізвище", "+38000112233", "Street 1", "Kyiv", "Kyiv", "01001", "UA"),
            "Leave at door",
            "idem-key-1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.CreatedOrders.Count);
        Assert.All(result.Value.CreatedOrders, x => Assert.Equal(OrderStatus.Pending, x.Status));
        Assert.Equal(3, orderItemRepo.Items.Count);
        Assert.Equal(2, orderAddressRepo.Items.Count);
        Assert.Empty((await cartItemRepo.ListByCartIdAsync(cart.Id, CancellationToken.None)));
        Assert.Contains($"cart:user:{userId}:active", cache.RemovedKeys);
    }

    [Fact]
    [Trait("Suite", "Notifications")]
    public async Task Checkout_Schedules_AdminNewOrder_For_Each_Company_Order()
    {
        var userId = Guid.NewGuid();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cartRepo = new InMemoryCartRepository();
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null));
        var cartItemRepo = new InMemoryCartItemRepository();
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 2, new Money(100), Money.Zero, now, now, false, null));
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(2), 1, new Money(80), Money.Zero, now, now, false, null));
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(3), 1, new Money(50), Money.Zero, now, now, false, null));

        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(ProductId.From(1), CompanyId.From(companyA), "A1", "a1", "d", new Money(100), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));
        products.Seed(Product.Reconstitute(ProductId.From(2), CompanyId.From(companyA), "A2", "a2", "d", new Money(80), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));
        products.Seed(Product.Reconstitute(ProductId.From(3), CompanyId.From(companyB), "B1", "b1", "d", new Money(50), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));

        var orderRepo = new InMemoryOrderRepository();
        var orderItemRepo = new InMemoryOrderItemRepository();
        var orderAddressRepo = new InMemoryOrderAddressRepository();
        var cache = new SpyCachePort();
        var spyNotifications = new SpyAppNotificationScheduler();

        var handler = new CheckoutCartCommandHandler(
            cartRepo,
            cartItemRepo,
            products,
            orderRepo,
            orderItemRepo,
            orderAddressRepo,
            new InMemoryPaymentRepository(),
            new FakeLiqPayPort(),
            cache,
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new InMemoryWarehouseStockRepository(),
            new WarehouseAllocationPlanner(new InMemoryWarehouseRepository(), new InMemoryWarehouseStockRepository()),
            spyNotifications,
            new NoopCartStockWatchRepository(),
            new FakeShippingMethodRepository(),
            new NoopCouponCheckoutService(),
            new NoopAppTransactionPort(),
            NullLogger<CheckoutCartCommandHandler>.Instance);

        var cmd = new CheckoutCartCommand(
            userId,
            CheckoutPaymentMethod.Card,
            1,
            new CheckoutAddressDto("Ім'я", "Прізвище", "+38000112233", "Street 1", "Kyiv", "Kyiv", "01001", "UA"),
            "Leave at door",
            "idem-admin-push-1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, spyNotifications.Requests.Count);
        Assert.Equal(2, spyNotifications.Requests.Count(r => r.TemplateKey == AppNotificationTemplateKeys.AdminNewOrder));
        Assert.Equal(2, spyNotifications.Requests.Count(r => r.TemplateKey == AppNotificationTemplateKeys.CompanyNewOrder));
        Assert.All(
            spyNotifications.Requests.Where(r => r.TemplateKey == AppNotificationTemplateKeys.AdminNewOrder),
            r =>
            {
                Assert.Equal(AppNotificationAudienceKind.Admins, r.Audience);
                Assert.True(r.Channels.HasFlag(AppNotificationChannelKind.Push));
                Assert.True(r.Channels.HasFlag(AppNotificationChannelKind.InApp));
            });
        foreach (var r in spyNotifications.Requests.Where(r => r.TemplateKey == AppNotificationTemplateKeys.CompanyNewOrder))
        {
            Assert.Equal(AppNotificationAudienceKind.CompanyStakeholders, r.Audience);
            Assert.NotNull(r.TargetCompanyId);
            Assert.True(r.Channels.HasFlag(AppNotificationChannelKind.Push));
            Assert.True(r.Channels.HasFlag(AppNotificationChannelKind.InApp));
        }

        var companyTargets = spyNotifications.Requests
            .Where(r => r.TemplateKey == AppNotificationTemplateKeys.CompanyNewOrder)
            .Select(r => r.TargetCompanyId!.Value)
            .ToHashSet();
        Assert.Contains(companyA, companyTargets);
        Assert.Contains(companyB, companyTargets);
    }

    [Fact]
    public async Task Checkout_Returns_Failure_For_Empty_Cart()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cartRepo = new InMemoryCartRepository();
        _ = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null));

        var handler = new CheckoutCartCommandHandler(
            cartRepo,
            new InMemoryCartItemRepository(),
            new InMemoryProductRepository(),
            new InMemoryOrderRepository(),
            new InMemoryOrderItemRepository(),
            new InMemoryOrderAddressRepository(),
            new InMemoryPaymentRepository(),
            new FakeLiqPayPort(),
            new SpyCachePort(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new InMemoryWarehouseStockRepository(),
            new WarehouseAllocationPlanner(new InMemoryWarehouseRepository(), new InMemoryWarehouseStockRepository()),
            new NoopAppNotificationScheduler(),
            new NoopCartStockWatchRepository(),
            new FakeShippingMethodRepository(),
            new NoopCouponCheckoutService(),
            new NoopAppTransactionPort(),
            NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(
            new CheckoutCartCommand(userId, CheckoutPaymentMethod.Card, 1, new CheckoutAddressDto("A", "B", "1", "S", "C", "ST", "P", "U"), null, "idem-key-2"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("empty", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_Returns_Failure_When_Stock_Is_Insufficient()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cartRepo = new InMemoryCartRepository();
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null));
        var cartItemRepo = new InMemoryCartItemRepository();
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 5, new Money(100), Money.Zero, now, now, false, null));

        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(ProductId.From(1), CompanyId.From(companyId), "A1", "a1", "d", new Money(100), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));

        var handler = new CheckoutCartCommandHandler(
            cartRepo,
            cartItemRepo,
            products,
            new InMemoryOrderRepository(),
            new InMemoryOrderItemRepository(),
            new InMemoryOrderAddressRepository(),
            new InMemoryPaymentRepository(),
            new FakeLiqPayPort(),
            new SpyCachePort(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new LowStockWarehouseStockRepository(available: 1),
            new WarehouseAllocationPlanner(new InMemoryWarehouseRepository(), new LowStockWarehouseStockRepository(available: 1)),
            new NoopAppNotificationScheduler(),
            new NoopCartStockWatchRepository(),
            new FakeShippingMethodRepository(),
            new NoopCouponCheckoutService(),
            new NoopAppTransactionPort(),
            NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(
            new CheckoutCartCommand(userId, CheckoutPaymentMethod.Card, 1, new CheckoutAddressDto("A", "B", "1", "S", "C", "ST", "P", "U"), null, "idem-stock-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("insufficient stock", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_Returns_Failure_When_Payment_Init_Throws()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cartRepo = new InMemoryCartRepository();
        var cart = await cartRepo.AddAsync(Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null));
        var cartItemRepo = new InMemoryCartItemRepository();
        _ = await cartItemRepo.AddAsync(CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(1), 1, new Money(100), Money.Zero, now, now, false, null));

        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(ProductId.From(1), CompanyId.From(companyId), "A1", "a1", "d", new Money(100), null, 10, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null));

        var orderRepo = new InMemoryOrderRepository();
        var handler = new CheckoutCartCommandHandler(
            cartRepo,
            cartItemRepo,
            products,
            orderRepo,
            new InMemoryOrderItemRepository(),
            new InMemoryOrderAddressRepository(),
            new InMemoryPaymentRepository(),
            new ThrowingLiqPayPort(),
            new SpyCachePort(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new InMemoryWarehouseStockRepository(),
            new WarehouseAllocationPlanner(new InMemoryWarehouseRepository(), new InMemoryWarehouseStockRepository()),
            new NoopAppNotificationScheduler(),
            new NoopCartStockWatchRepository(),
            new FakeShippingMethodRepository(),
            new NoopCouponCheckoutService(),
            new NoopAppTransactionPort(),
            NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(
            new CheckoutCartCommand(userId, CheckoutPaymentMethod.Card, 1, new CheckoutAddressDto("A", "B", "1", "S", "C", "ST", "P", "U"), null, "idem-payment-throw"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("failed to checkout", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
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
            var stored = Cart.Reconstitute(CartId.From(id), cart.UserId, cart.Status, cart.LastActivityAt, cart.CreatedAt, cart.UpdatedAt, cart.IsDeleted, cart.DeletedAt);
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

        public Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.CartId == cartId && x.ProductId == productId && !x.IsDeleted));
        public Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CartItem>>(_items.Values.Where(x => x.CartId == cartId && !x.IsDeleted).ToList());

        public Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default)
        {
            var id = item.Id.Value <= 0 ? _nextId++ : item.Id.Value;
            var stored = CartItem.Reconstitute(CartItemId.From(id), item.CartId, item.ProductId, item.Quantity, item.PriceAtMoment, item.Discount, item.CreatedAt, item.UpdatedAt, item.IsDeleted, item.DeletedAt);
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
            if (_items.TryGetValue(id.Value, out var item))
                _items[id.Value] = CartItem.Reconstitute(item.Id, item.CartId, item.ProductId, item.Quantity, item.PriceAtMoment, item.Discount, item.CreatedAt, utcNow, true, utcNow);
            return Task.CompletedTask;
        }

        public Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default)
        {
            foreach (var item in _items.Values.Where(x => x.CartId == cartId && !x.IsDeleted).ToList())
                _items[item.Id.Value] = CartItem.Reconstitute(item.Id, item.CartId, item.ProductId, item.Quantity, item.PriceAtMoment, item.Discount, item.CreatedAt, utcNow, true, utcNow);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = new();
        public void Seed(Product product) => _items[product.Id.Value] = product;
        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.CompanyId == companyId && x.Slug == slug));
        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.Slug == slug));
        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
        {
            var set = ids.Select(x => x.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => set.Contains(x.Id.Value)).ToList());
        }
        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.CompanyId == companyId).ToList());
        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.Active && !x.IsDeleted).ToList());
        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.PendingReview && !x.IsDeleted).ToList());
        public Task AddAsync(Product product, CancellationToken ct = default) { Seed(product); return Task.CompletedTask; }
        public Task UpdateAsync(Product product, CancellationToken ct = default) { Seed(product); return Task.CompletedTask; }
    }

    private sealed class InMemoryWarehouseRepository : Marketplace.Domain.Inventory.Repositories.IWarehouseRepository
    {
        public Task<Marketplace.Domain.Inventory.Entities.Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default)
        {
            var companyId = CompanyId.From(Guid.NewGuid());
            return Task.FromResult<Marketplace.Domain.Inventory.Entities.Warehouse?>(
                Marketplace.Domain.Inventory.Entities.Warehouse.Reconstitute(
                    id,
                    companyId,
                    "WH",
                    "WH",
                    Address.Empty,
                    "UTC",
                    1,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null));
        }

        public Task<IReadOnlyList<Marketplace.Domain.Inventory.Entities.Warehouse>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Inventory.Entities.Warehouse>>([
                Marketplace.Domain.Inventory.Entities.Warehouse.Reconstitute(
                    WarehouseId.From(1),
                    companyId,
                    "WH",
                    "WH",
                    Address.Empty,
                    "UTC",
                    1,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null)
            ]);

        public Task AddAsync(Marketplace.Domain.Inventory.Entities.Warehouse warehouse, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Inventory.Entities.Warehouse warehouse, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryWarehouseStockRepository : Marketplace.Domain.Inventory.Repositories.IWarehouseStockRepository
    {
        public Task<Marketplace.Domain.Inventory.Entities.WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<Marketplace.Domain.Inventory.Entities.WarehouseStock?>(Marketplace.Domain.Inventory.Entities.WarehouseStock.Reconstitute(
                WarehouseStockId.From(1),
                CompanyId.From(Guid.NewGuid()),
                warehouseId,
                productId,
                1000,
                0,
                0,
                1,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null));

        public Task<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>>([]);

        public Task<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>>([
                Marketplace.Domain.Inventory.Entities.WarehouseStock.Reconstitute(
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

        public Task AddAsync(Marketplace.Domain.Inventory.Entities.WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Inventory.Entities.WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class LowStockWarehouseStockRepository : Marketplace.Domain.Inventory.Repositories.IWarehouseStockRepository
    {
        private readonly int _available;

        public LowStockWarehouseStockRepository(int available) => _available = available;

        public Task<Marketplace.Domain.Inventory.Entities.WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<Marketplace.Domain.Inventory.Entities.WarehouseStock?>(null);

        public Task<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>>([]);

        public Task<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Marketplace.Domain.Inventory.Entities.WarehouseStock>>([
                Marketplace.Domain.Inventory.Entities.WarehouseStock.Reconstitute(
                    WarehouseStockId.From(1),
                    companyId,
                    WarehouseId.From(1),
                    productId,
                    _available,
                    0,
                    0,
                    1,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null)
            ]);

        public Task AddAsync(Marketplace.Domain.Inventory.Entities.WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Inventory.Entities.WarehouseStock stock, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Dictionary<long, Order> _items = new();
        private long _nextId = 1;

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>(_items.Values.Where(x => x.CustomerId == customerId).ToList());
        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default)
        {
            IEnumerable<Order> q = _items.Values;
            if (filter.CustomerId.HasValue)
                q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
            if (filter.CompanyId.HasValue)
                q = q.Where(x => x.CompanyId.Value == filter.CompanyId.Value);
            var list = q.ToList();
            return Task.FromResult(((IReadOnlyList<Order>)list, (long)list.Count));
        }
        public Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            var id = order.Id.Value <= 0 ? _nextId++ : order.Id.Value;
            var saved = Order.Reconstitute(OrderId.From(id), order.OrderNumber, order.CustomerId, order.CompanyId, order.Status, order.TotalPrice, order.Subtotal, order.ShippingCost, order.DiscountAmount, order.TaxAmount, order.ShippingMethodId, order.PaymentMethod, order.Notes, order.TrackingNumber, order.ShippedAt, order.DeliveredAt, order.CancelledAt, order.RefundedAt, order.CreatedAt, order.UpdatedAt, order.IsDeleted, order.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            _items[order.Id.Value] = order;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly Dictionary<long, Payment> _items = new();
        private long _nextId = 1;

        public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.OrderId == orderId));
        public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.TransactionId == transactionId));
        public Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Payment>>(_items.Values.Where(x => x.Status == status).ToList());
        public Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
        {
            var id = payment.Id.Value <= 0 ? _nextId++ : payment.Id.Value;
            var saved = Payment.Reconstitute(PaymentId.From(id), payment.OrderId, payment.PaymentMethod, payment.Amount, payment.Currency, payment.TransactionId, payment.Status, payment.ProviderResponse, payment.ProcessedAt, payment.CreatedAt, payment.UpdatedAt, payment.IsDeleted, payment.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }
        public Task UpdateAsync(Payment payment, CancellationToken ct = default)
        {
            _items[payment.Id.Value] = payment;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLiqPayPort : ILiqPayPort
    {
        public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayCreatePaymentResult(true, request.OrderNumber, "https://liqpay.test", "data", "sig", "{\"status\":\"ok\"}", null));
        public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default) => Task.FromResult(true);
        public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default) => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, "success", "{}", null));
        public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default) => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));
        public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default) => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));
        public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
    }

    private sealed class ThrowingLiqPayPort : ILiqPayPort
    {
        public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
            => throw new InvalidOperationException("payment init failed");

        public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default) => Task.FromResult(true);
        public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default) => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, "success", "{}", null));
        public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default) => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));
        public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default) => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));
        public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
    }

    private sealed class InMemoryOrderItemRepository : IOrderItemRepository
    {
        public List<OrderItem> Items { get; } = [];
        public Task<IReadOnlyList<OrderItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OrderItem>>(Items.Where(x => x.OrderId == orderId).ToList());
        public Task AddRangeAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default) { Items.AddRange(items); return Task.CompletedTask; }
    }

    private sealed class InMemoryOrderAddressRepository : IOrderAddressSnapshotRepository
    {
        public List<OrderAddressSnapshot> Items { get; } = [];
        public Task<IReadOnlyList<OrderAddressSnapshot>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OrderAddressSnapshot>>(Items.Where(x => x.OrderId == orderId).ToList());
        public Task AddRangeAsync(IReadOnlyList<OrderAddressSnapshot> addresses, CancellationToken ct = default) { Items.AddRange(addresses); return Task.CompletedTask; }
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public List<string> RemovedKeys { get; } = [];
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) { RemovedKeys.Add(key); return Task.CompletedTask; }
    }

    private sealed class NoopOrderCacheInvalidationService : IOrderCacheInvalidationService
    {
        public Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default) => Task.FromResult(1L);
        public Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
    }

    private sealed class NoopOrderStatusHistoryWriter : Marketplace.Application.Orders.Services.IOrderStatusHistoryWriter
    {
        public Task RecordCreatedAsync(Order order, Guid actorUserId, string source, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoopAppNotificationScheduler : IAppNotificationScheduler
    {
        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
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

    private sealed class FakeShippingMethodRepository : IShippingMethodRepository
    {
        public Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken ct = default)
            => Task.FromResult<ShippingMethod?>(
                ShippingMethod.Reconstitute(
                    id,
                    "Nova Poshta",
                    Marketplace.Domain.Shipping.Enums.ShippingCarrierCode.NovaPoshta,
                    new Money(99),
                    null,
                    1,
                    3,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null));

        public Task<IReadOnlyList<ShippingMethod>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShippingMethod>>([]);
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

    private sealed class NoopAppTransactionPort : IAppTransactionPort
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);
    }
}
