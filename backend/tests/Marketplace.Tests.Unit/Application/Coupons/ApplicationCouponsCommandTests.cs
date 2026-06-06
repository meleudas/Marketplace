using Marketplace.Application.Coupons.Commands.ApplyCouponToCart;
using Marketplace.Application.Coupons.Commands.CreateCoupon;
using Marketplace.Application.Coupons.Commands.ValidateCouponForCart;
using Marketplace.Application.Coupons.Services;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Coupons")]
public sealed class ApplicationCouponsCommandTests
{
    [Fact]
    public async Task ValidateCouponForCart_Returns_Discount_For_Valid_Coupon()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var cartRepo = new InMemoryCartRepository(userId, now);
        var cartItemRepo = new InMemoryCartItemRepository(cartRepo.CartId, now);
        var productRepo = new InMemoryProductRepository(companyId, now);
        var couponRepo = new InMemoryCouponRepository(now, companyId);
        var usageRepo = new InMemoryCouponUsageRepository();

        var service = new CouponCartValidationService(cartRepo, cartItemRepo, productRepo, couponRepo, usageRepo);
        var handler = new ValidateCouponForCartCommandHandler(service);

        var result = await handler.Handle(new ValidateCouponForCartCommand(userId, "SAVE10"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value!.DiscountAmount);
    }

    [Fact]
    public async Task ApplyCouponToCart_Stores_Link()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var cartRepo = new InMemoryCartRepository(userId, now);
        var cartItemRepo = new InMemoryCartItemRepository(cartRepo.CartId, now);
        var productRepo = new InMemoryProductRepository(companyId, now);
        var couponRepo = new InMemoryCouponRepository(now, companyId);
        var usageRepo = new InMemoryCouponUsageRepository();
        var linkRepo = new InMemoryCartCouponLinkRepository();

        var service = new CouponCartValidationService(cartRepo, cartItemRepo, productRepo, couponRepo, usageRepo);
        var handler = new ApplyCouponToCartCommandHandler(service, linkRepo);

        var result = await handler.Handle(new ApplyCouponToCartCommand(userId, "SAVE10"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(await linkRepo.GetByCartIdAsync(cartRepo.CartId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCoupon_Returns_Conflict_When_Code_Exists()
    {
        var repo = new InMemoryCouponRepository(DateTime.UtcNow, Guid.NewGuid());
        var handler = new CreateCouponCommandHandler(repo);

        var result = await handler.Handle(
            new CreateCouponCommand("SAVE10", null, 10, "Percentage", null, null, 1, null, null, true, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("conflict", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly Cart _cart;
        public CartId CartId => _cart.Id;

        public InMemoryCartRepository(Guid userId, DateTime now)
        {
            _cart = Cart.Reconstitute(CartId.From(1), userId, CartStatus.Active, now, now, now, false, null);
        }

        public Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<Cart?>(_cart.UserId == userId ? _cart : null);
        public Task<Cart?> GetByIdAsync(CartId id, CancellationToken ct = default)
            => Task.FromResult<Cart?>(_cart.Id == id ? _cart : null);
        public Task<Cart> AddAsync(Cart cart, CancellationToken ct = default) => Task.FromResult(cart);
        public Task UpdateAsync(Cart cart, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCartItemRepository : ICartItemRepository
    {
        private readonly CartItem _item;

        public InMemoryCartItemRepository(CartId cartId, DateTime now)
        {
            _item = CartItem.Reconstitute(CartItemId.From(1), cartId, ProductId.From(100), 1, new Money(100), Money.Zero, now, now, false, null);
        }

        public Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default) => Task.FromResult<CartItem?>(_item);
        public Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default) => Task.FromResult<CartItem?>(_item);
        public Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CartItem>>([_item]);
        public Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default) => Task.FromResult(item);
        public Task UpdateAsync(CartItem item, CancellationToken ct = default) => Task.CompletedTask;
        public Task SoftDeleteAsync(CartItemId id, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
        public Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Product _product;

        public InMemoryProductRepository(Guid companyId, DateTime now)
        {
            _product = Product.Reconstitute(
                ProductId.From(100),
                CompanyId.From(companyId),
                "P",
                "p",
                "d",
                new Money(100),
                null,
                10,
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

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) => Task.FromResult<Product?>(_product);
        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default) => Task.FromResult<Product?>(_product);
        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult<Product?>(_product);
        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([_product]);
        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([_product]);
        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([_product]);
        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task AddAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCouponRepository : ICouponRepository
    {
        private Coupon _coupon;

        public InMemoryCouponRepository(DateTime now, Guid companyId)
        {
            _coupon = Coupon.Reconstitute(
                CouponId.From(1),
                "SAVE10",
                null,
                new Money(10),
                Marketplace.Domain.Coupons.Enums.DiscountType.Fixed,
                null,
                10,
                0,
                1,
                now.AddDays(7),
                now.AddDays(-1),
                null,
                null,
                new JsonBlob($"[\"{companyId}\"]"),
                true,
                now,
                now,
                false,
                null);
        }

        public Task<Coupon?> GetByIdAsync(CouponId id, CancellationToken ct = default) => Task.FromResult<Coupon?>(_coupon);
        public Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
            => Task.FromResult<Coupon?>(string.Equals(_coupon.Code, code, StringComparison.OrdinalIgnoreCase) ? _coupon : null);
        public Task<Coupon> AddAsync(Coupon entity, CancellationToken ct = default)
        {
            _coupon = entity;
            return Task.FromResult(entity);
        }
        public Task UpdateAsync(Coupon entity, CancellationToken ct = default)
        {
            _coupon = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCouponUsageRepository : ICouponUsageRepository
    {
        public Task<int> CountByCouponAndUserAsync(CouponId couponId, Guid userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> ExistsByCouponAndOrderAsync(CouponId couponId, OrderId orderId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<CouponUsage> AddAsync(CouponUsage entity, CancellationToken ct = default) => Task.FromResult(entity);
    }

    private sealed class InMemoryCartCouponLinkRepository : ICartCouponLinkRepository
    {
        private CartCouponLink? _value;

        public Task<CartCouponLink?> GetByCartIdAsync(CartId cartId, CancellationToken ct = default) => Task.FromResult(_value);
        public Task<CartCouponLink> UpsertAsync(CartCouponLink entity, CancellationToken ct = default)
        {
            _value = entity;
            return Task.FromResult(entity);
        }

        public Task RemoveByCartIdAsync(CartId cartId, CancellationToken ct = default)
        {
            _value = null;
            return Task.CompletedTask;
        }
    }
}
