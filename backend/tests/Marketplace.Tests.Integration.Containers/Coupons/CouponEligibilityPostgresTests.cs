using Marketplace.Application.Coupons.Commands.ApplyCouponToCart;
using Marketplace.Application.Coupons.Commands.CreateCoupon;
using Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;
using Marketplace.Application.Coupons.Commands.ValidateCouponForCart;
using Marketplace.Application.Coupons.Options;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Coupons.Validation;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Coupons;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Coupons")]
[Trait("Layer", "IntegrationContainers")]
public sealed class CouponEligibilityPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public CouponEligibilityPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Validate_Apply_And_Consume_Coupon_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fixture = await SeedFixtureAsync(db);

        var couponRepo = new CouponRepository(db);
        var usageRepo = new CouponUsageRepository(db);
        var linkRepo = new CartCouponLinkRepository(db);
        var evaluator = new CouponEligibilityEvaluator(usageRepo,
        [
            new ActiveWindowCouponRule(),
            new CompanyScopeCouponRule(),
            new UsageLimitsCouponRule(),
            new MinOrderAmountCouponRule()
        ]);
        var validation = new CouponCartValidationService(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            couponRepo,
            evaluator);

        var create = new CreateCouponCommandHandler(couponRepo);
        var created = await create.Handle(
            new CreateCouponCommand("PGSAVE20", null, 20, "Fixed", 50, 100, 2, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(2), true, $"[\"{fixture.CompanyId}\"]", null, null),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var validate = new ValidateCouponForCartCommandHandler(validation);
        var validated = await validate.Handle(new ValidateCouponForCartCommand(fixture.UserId, "PGSAVE20"), CancellationToken.None);
        Assert.True(validated.IsSuccess);

        var apply = new ApplyCouponToCartCommandHandler(validation, linkRepo);
        var applied = await apply.Handle(new ApplyCouponToCartCommand(fixture.UserId, "PGSAVE20"), CancellationToken.None);
        Assert.True(applied.IsSuccess);

        var checkout = new CouponCheckoutService(linkRepo, couponRepo, usageRepo, validation, evaluator, Options.Create(new CouponsOptions
        {
            ReadEnabled = true,
            CheckoutConsumeEnabled = true
        }));
        var discount = await checkout.ResolveDiscountAsync(fixture.UserId, CartId.From(fixture.CartId), CompanyId.From(fixture.CompanyId), 100, CancellationToken.None);
        Assert.Equal(20m, discount.DiscountAmount);
        await checkout.ConsumeAsync(fixture.UserId, OrderId.From(999), discount.CouponId!.Value, discount.CouponCode!, discount.DiscountAmount, CancellationToken.None);
        Assert.Equal(1, await db.CouponUsages.AsNoTracking().CountAsync());

        var remove = new RemoveCouponFromCartCommandHandler(new CartRepository(db), linkRepo);
        var removed = await remove.Handle(new RemoveCouponFromCartCommand(fixture.UserId, "PGSAVE20"), CancellationToken.None);
        Assert.True(removed.IsSuccess);
    }

    private static async Task<(Guid UserId, Guid CompanyId, long CartId)> SeedFixtureAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        const long productId = 88001;

        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(Product.Reconstitute(
            ProductId.From(productId), CompanyId.From(companyId), "Coupon Product", "coupon-product", "d",
            new Money(100), null, 1, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null), CancellationToken.None);

        var cartRepo = new CartRepository(db);
        var cart = await cartRepo.AddAsync(
            Marketplace.Domain.Cart.Entities.Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null),
            CancellationToken.None);
        await new CartItemRepository(db).AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(productId), 1, new Money(100), Money.Zero, now, now, false, null),
            CancellationToken.None);
        return (userId, companyId, cart.Id.Value);
    }
}
