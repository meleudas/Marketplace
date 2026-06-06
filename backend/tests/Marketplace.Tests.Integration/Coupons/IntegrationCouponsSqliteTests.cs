using Marketplace.Application.Coupons.Commands.ApplyCouponToCart;
using Marketplace.Application.Coupons.Commands.CreateCoupon;
using Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;
using Marketplace.Application.Coupons.Commands.ValidateCouponForCart;
using Marketplace.Application.Coupons.Options;
using Marketplace.Application.Coupons.Services;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Coupons")]
public sealed class IntegrationCouponsSqliteTests
{
    [Fact]
    public async Task Validate_Apply_Consume_Remove_Flow_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var fixture = await SeedFixtureAsync(db);

        var couponRepo = new CouponRepository(db);
        var usageRepo = new CouponUsageRepository(db);
        var linkRepo = new CartCouponLinkRepository(db);
        var validation = new CouponCartValidationService(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            couponRepo,
            usageRepo);

        var create = new CreateCouponCommandHandler(couponRepo);
        var created = await create.Handle(
            new CreateCouponCommand("SAVE20", null, 20, "Fixed", 50, 100, 2, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(2), true, $"[\"{fixture.CompanyId}\"]"),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var validate = new ValidateCouponForCartCommandHandler(validation);
        var validated = await validate.Handle(new ValidateCouponForCartCommand(fixture.UserId, "SAVE20"), CancellationToken.None);
        Assert.True(validated.IsSuccess);
        Assert.Equal(20m, validated.Value!.DiscountAmount);

        var apply = new ApplyCouponToCartCommandHandler(validation, linkRepo);
        var applied = await apply.Handle(new ApplyCouponToCartCommand(fixture.UserId, "SAVE20"), CancellationToken.None);
        Assert.True(applied.IsSuccess);

        var checkout = new CouponCheckoutService(linkRepo, couponRepo, usageRepo, Options.Create(new CouponsOptions
        {
            ReadEnabled = true,
            CheckoutConsumeEnabled = true
        }));
        var discount = await checkout.ResolveDiscountAsync(fixture.UserId, CartId.From(fixture.CartId), CompanyId.From(fixture.CompanyId), 100, CancellationToken.None);
        Assert.Equal(20m, discount.DiscountAmount);
        await checkout.ConsumeAsync(
            fixture.UserId,
            OrderId.From(999),
            discount.CouponId!.Value,
            discount.CouponCode!,
            discount.DiscountAmount,
            CancellationToken.None);

        var usageCount = await db.CouponUsages.AsNoTracking().CountAsync();
        Assert.Equal(1, usageCount);

        var remove = new RemoveCouponFromCartCommandHandler(new CartRepository(db), linkRepo);
        var removed = await remove.Handle(new RemoveCouponFromCartCommand(fixture.UserId, "SAVE20"), CancellationToken.None);
        Assert.True(removed.IsSuccess);
    }

    private static async Task<(Guid UserId, Guid CompanyId, long CartId)> SeedFixtureAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(
            Product.Reconstitute(
                ProductId.From(3001),
                CompanyId.From(companyId),
                "Coupon Product",
                "coupon-product",
                "Desc",
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
                null),
            CancellationToken.None);

        var cartRepo = new CartRepository(db);
        var cart = await cartRepo.AddAsync(
            Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null),
            CancellationToken.None);

        var cartItemRepo = new CartItemRepository(db);
        await cartItemRepo.AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(3001), 1, new Money(100), Money.Zero, now, now, false, null),
            CancellationToken.None);

        await db.SaveChangesAsync();
        return (userId, companyId, cart.Id.Value);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
