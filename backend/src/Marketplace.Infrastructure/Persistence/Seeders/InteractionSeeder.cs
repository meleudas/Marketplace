using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class InteractionSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Coupons.AnyAsync(ct) || await context.Carts.AnyAsync(ct))
            return;

        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var buyers = await context.MarketplaceUsers.Where(u => u.Role == 1).ToListAsync(ct);
        var sellers = await context.MarketplaceUsers.Where(u => u.Role == 2).ToListAsync(ct);
        var products = await context.Products.ToListAsync(ct);
        var companies = await context.Companies.ToListAsync(ct);
        var orders = await context.Orders.ToListAsync(ct);
        var companyMembers = await context.CompanyMembers.ToListAsync(ct);

        var coupons = new List<CouponRecord>
        {
            new() { Code = "BOOK10", Description = "Знижка 10% на всі книги", DiscountType = 1, DiscountAmount = 10, MinOrderAmount = 300, UsageLimit = 100, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(3), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "SUMMER25", Description = "Літня знижка 25%", DiscountType = 1, DiscountAmount = 25, MinOrderAmount = 500, UsageLimit = 50, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(2), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "NEWUSER", Description = "Для нових користувачів", DiscountType = 2, DiscountAmount = 100, MinOrderAmount = 200, UsageLimit = 200, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(6), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "FREESHIP", Description = "Безкоштовна доставка", DiscountType = 3, DiscountAmount = 80, MinOrderAmount = 600, UsageLimit = 75, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(1), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "WELCOME", Description = "Вітальний купон 15%", DiscountType = 1, DiscountAmount = 15, MinOrderAmount = 250, UsageLimit = 500, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(3), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "CLASSIC20", Description = "Знижка на класику 20%", DiscountType = 1, DiscountAmount = 20, MinOrderAmount = 200, UsageLimit = 100, UserUsageLimit = 1, ExpiresAtUtc = now.AddMonths(2), IsActive = true, CreatedAt = now, UpdatedAt = now },
        };
        context.Coupons.AddRange(coupons);
        await context.SaveChangesAsync(ct);

        var discountedOrders = orders.Where(_ => rng.Next(3) == 0).Take(3).ToList();
        foreach (var order in discountedOrders)
        {
            var coupon = coupons[rng.Next(coupons.Count)];
            context.CouponUsages.Add(new CouponUsageRecord
            {
                CouponId = coupon.Id, UserId = order.CustomerId, OrderId = order.Id,
                CouponCode = coupon.Code, DiscountAppliedAmount = order.DiscountAmount,
                ConsumedAtUtc = order.CreatedAt, CreatedAt = now, UpdatedAt = now,
            });
        }

        var selectedBuyers = buyers.OrderBy(_ => rng.Next()).Take(40).ToList();
        var carts = new List<CartRecord>();
        var cartItems = new List<CartItemRecord>();
        foreach (var buyer in selectedBuyers)
        {
            carts.Add(new CartRecord
            {
                UserId = buyer.Id,
                Status = 1,
                LastActivityAt = now.AddDays(-rng.Next(0, 14)),
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        context.Carts.AddRange(carts);
        await context.SaveChangesAsync(ct);

        for (var cartIndex = 0; cartIndex < carts.Count; cartIndex++)
        {
            var cartId = carts[cartIndex].Id;
            foreach (var product in products.OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)))
            {
                cartItems.Add(new CartItemRecord
                {
                    CartId = cartId,
                    ProductId = product.Id,
                    Quantity = rng.Next(1, 3),
                    PriceAtMoment = product.Price,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        context.CartItems.AddRange(cartItems);

        var favorites = new HashSet<(Guid UserId, long ProductId)>();
        foreach (var buyer in buyers)
            foreach (var product in products.OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)))
                favorites.Add((buyer.Id, product.Id));

        context.Favorites.AddRange(favorites.Select(f => new FavoriteRecord
        {
            UserId = f.UserId, ProductId = f.ProductId, AddedAt = now.AddDays(-rng.Next(0, 60)),
            PriceAtAdd = products.First(p => p.Id == f.ProductId).Price, IsAvailable = true,
            CreatedAt = now, UpdatedAt = now,
        }));

        var reviewTexts = new[] { "Чудова книга, рекомендую!", "Сподобалась, якісне видання", "Дуже задоволений покупкою", "Гарна якість друку", "Швидка доставка, книга супер", "Не сподобалась обкладинка", "Чудовий переклад" };
        foreach (var order in orders)
        {
            var orderItems = await context.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync(ct);
            foreach (var item in orderItems.Where(_ => rng.Next(2) == 0))
            {
                context.ProductReviews.Add(new ProductReviewRecord
                {
                    ProductId = item.ProductId, UserId = order.CustomerId, UserName = "Покупець",
                    Rating = (byte)rng.Next(3, 6), Comment = reviewTexts[rng.Next(reviewTexts.Length)],
                    IsVerifiedPurchase = true, OrderId = order.Id, Status = 1,
                    CreatedAt = order.DeliveredAt ?? now, UpdatedAt = order.DeliveredAt ?? now,
                });
            }
        }

        await context.SaveChangesAsync(ct);

        var recentReviews = await context.ProductReviews.OrderByDescending(r => r.Id).Take(5).ToListAsync(ct);
        foreach (var review in recentReviews)
        {
            var product = products.FirstOrDefault(p => p.Id == review.ProductId);
            if (product == null) continue;
            var member = companyMembers.FirstOrDefault(cm => cm.CompanyId == product.CompanyId);
            if (member == null) continue;

            context.ReviewReplies.Add(new ReviewReplyRecord
            {
                ProductReviewId = review.Id, CompanyId = product.CompanyId,
                AuthorUserId = member.UserId, Body = "Дякуємо за відгук! Раді, що книга сподобалась.",
                CreatedAt = now, UpdatedAt = now,
            });
        }

        await context.SaveChangesAsync(ct);
    }
}
