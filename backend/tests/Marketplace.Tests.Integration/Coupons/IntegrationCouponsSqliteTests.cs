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
using Marketplace.Domain.Coupons.Repositories;
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
        var evaluator = CreateEvaluator(usageRepo);
        var validation = new CouponCartValidationService(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            couponRepo,
            evaluator);

        var create = new CreateCouponCommandHandler(couponRepo);
        var created = await create.Handle(
            new CreateCouponCommand("SAVE20", null, 20, "Fixed", 50, 100, 2, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(2), true, $"[\"{fixture.CompanyId}\"]", null, null),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var validate = new ValidateCouponForCartCommandHandler(validation);
        var validated = await validate.Handle(new ValidateCouponForCartCommand(fixture.UserId, "SAVE20"), CancellationToken.None);
        Assert.True(validated.IsSuccess);
        Assert.Equal(20m, validated.Value!.DiscountAmount);

        var apply = new ApplyCouponToCartCommandHandler(validation, linkRepo);
        var applied = await apply.Handle(new ApplyCouponToCartCommand(fixture.UserId, "SAVE20"), CancellationToken.None);
        Assert.True(applied.IsSuccess);

        var checkout = new CouponCheckoutService(linkRepo, couponRepo, usageRepo, validation, evaluator, Options.Create(new CouponsOptions
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

    [Fact]
    public async Task ConsumeOnceAsync_Records_Single_Usage_And_Removes_Link()
    {
        await using var db = await CreateSqliteContextAsync();
        var fixture = await SeedFixtureAsync(db);

        var couponRepo = new CouponRepository(db);
        var usageRepo = new CouponUsageRepository(db);
        var linkRepo = new CartCouponLinkRepository(db);
        var evaluator = CreateEvaluator(usageRepo);
        var validation = new CouponCartValidationService(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            couponRepo,
            evaluator);
        var checkout = new CouponCheckoutService(linkRepo, couponRepo, usageRepo, validation, evaluator, Options.Create(new CouponsOptions
        {
            ReadEnabled = true,
            CheckoutConsumeEnabled = true
        }));

        var create = new CreateCouponCommandHandler(couponRepo);
        var created = await create.Handle(
            new CreateCouponCommand("ONCE10", null, 10, "Fixed", null, 10, 1, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(2), true, $"[\"{fixture.CompanyId}\"]", null, null),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var apply = new ApplyCouponToCartCommandHandler(validation, linkRepo);
        await apply.Handle(new ApplyCouponToCartCommand(fixture.UserId, "ONCE10"), CancellationToken.None);

        await checkout.ConsumeOnceAsync(
            fixture.UserId,
            OrderId.From(1001),
            CartId.From(fixture.CartId),
            created.Value!.Id,
            "ONCE10",
            10m,
            CancellationToken.None);

        Assert.Equal(1, await db.CouponUsages.AsNoTracking().CountAsync());
        Assert.Equal(1, await db.Coupons.AsNoTracking().Select(x => x.UsageCount).FirstAsync());
        Assert.Null(await linkRepo.GetByCartIdAsync(CartId.From(fixture.CartId), CancellationToken.None));

        await checkout.ConsumeOnceAsync(
            fixture.UserId,
            OrderId.From(1001),
            CartId.From(fixture.CartId),
            created.Value!.Id,
            "ONCE10",
            10m,
            CancellationToken.None);

        Assert.Equal(1, await db.CouponUsages.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Category_Scoped_Coupon_Discounts_Only_Matching_Lines()
    {
        await using var db = await CreateSqliteContextAsync();
        var fixture = await SeedFixtureAsync(db);

        var couponRepo = new CouponRepository(db);
        var usageRepo = new CouponUsageRepository(db);
        var evaluator = CreateEvaluator(usageRepo);
        var validation = new CouponCartValidationService(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            couponRepo,
            evaluator);

        var create = new CreateCouponCommandHandler(couponRepo);
        await create.Handle(
            new CreateCouponCommand("CAT5", null, 10, "Percentage", null, 10, 1, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(2), true, $"[\"{fixture.CompanyId}\"]", "[2]", null),
            CancellationToken.None);

        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(
            Product.Reconstitute(
                ProductId.From(3002),
                CompanyId.From(fixture.CompanyId),
                "Other category product",
                "other-cat",
                "Desc",
                new Money(100),
                null,
                10,
                0,
                CategoryId.From(2),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null),
            CancellationToken.None);

        var cartItemRepo = new CartItemRepository(db);
        await cartItemRepo.AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), CartId.From(fixture.CartId), ProductId.From(3002), 1, new Money(100), Money.Zero, DateTime.UtcNow, DateTime.UtcNow, false, null),
            CancellationToken.None);
        await db.SaveChangesAsync();

        var validated = await validation.ValidateAsync(fixture.UserId, "CAT5", CancellationToken.None);

        Assert.True(validated.Result.IsValid);
        Assert.Equal(10m, validated.Result.DiscountAmount);
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

    private static CouponEligibilityEvaluator CreateEvaluator(ICouponUsageRepository usageRepo) =>
        new(usageRepo,
        [
            new ActiveWindowCouponRule(),
            new CompanyScopeCouponRule(),
            new UsageLimitsCouponRule(),
            new MinOrderAmountCouponRule()
        ]);
}
