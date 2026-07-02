using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class FinalBatchSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.UserAddresses.AnyAsync(ct))
            return;

        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var buyers = await context.MarketplaceUsers.Where(u => u.Role == 1).ToListAsync(ct);
        var companies = await context.Companies.ToListAsync(ct);
        var products = await context.Products.ToListAsync(ct);
        var carts = await context.Carts.ToListAsync(ct);
        var coupons = await context.Coupons.ToListAsync(ct);
        var admin = await context.MarketplaceUsers.FirstAsync(u => u.Role == 4, ct);
        var warehouses = await context.Warehouses.ToListAsync(ct);
        var orders = await context.Orders.ToListAsync(ct);
        var orderItems = await context.OrderItems.ToListAsync(ct);

        foreach (var buyer in buyers.Take(30))
        {
            context.UserAddresses.Add(new UserAddressRecord
            {
                UserId = buyer.Id, Type = 1, IsDefault = true,
                FirstName = buyer.FirstName, LastName = buyer.LastName,
                Phone = $"+38050{rng.Next(1000000, 9999999)}",
                Street = $"вул. {new[] { "Шевченка", "Франка", "Грушевського", "Хрещатик", "Сагайдачного" }[rng.Next(5)]}, {rng.Next(1, 100)}",
                City = new[] { "Київ", "Львів", "Харків", "Одеса", "Дніпро" }[rng.Next(5)],
                State = "Україна", PostalCode = $"{rng.Next(1000, 99999)}", Country = "Україна",
                CreatedAt = now, UpdatedAt = now,
            });
        }

        foreach (var cart in carts.Take(5))
        {
            var coupon = coupons[rng.Next(coupons.Count)];
            context.CartCouponLinks.Add(new CartCouponLinkRecord
            {
                CartId = cart.Id, CouponId = coupon.Id, CouponCode = coupon.Code,
                AppliedAtUtc = now, CreatedAt = now, UpdatedAt = now,
            });
        }

        foreach (var product in products.OrderBy(_ => rng.Next()).Take(20))
            context.CartStockWatches.Add(new CartStockWatchRecord
            {
                UserId = buyers[rng.Next(buyers.Count)].Id, ProductId = product.Id,
                CreatedAtUtc = now,
            });

        foreach (var company in companies.Take(10))
        {
            var buyer = buyers[rng.Next(buyers.Count)];
            context.CompanyReviews.Add(new CompanyReviewRecord
            {
                CompanyId = company.Id, UserId = buyer.Id, UserName = $"{buyer.FirstName} {buyer.LastName}",
                OverallRating = (decimal)Math.Round(3 + rng.NextDouble() * 2, 1), Status = 1,
                Comment = new[] { "Гарний магазин", "Швидка доставка", "Рекомендую", "Якісні книги", "Чудовий вибір" }[rng.Next(5)],
                CreatedAt = now.AddDays(-rng.Next(10, 60)), UpdatedAt = now,
            });
        }

        foreach (var buyer in buyers.Take(10))
        {
            context.Notifications.Add(new NotificationRecord
            {
                UserId = buyer.Id, Type = 1, Title = "Замовлення прибуло!",
                Message = "Ваше замовлення вже у відділенні Нової Пошти.",
                Data = "{}", IsRead = rng.Next(2) == 0, CreatedAt = now.AddDays(-rng.Next(1, 10)), UpdatedAt = now,
            });
        }

        var opId = 0;
        foreach (var order in orders.Take(5))
        {
            var wh = warehouses.FirstOrDefault(w => w.CompanyId == order.CompanyId);
            if (wh == null) continue;
            var items = orderItems.Where(oi => oi.OrderId == order.Id).ToList();

            var reservation = new InventoryReservationRecord
            {
                CompanyId = order.CompanyId, WarehouseId = wh.Id,
                ProductId = items.First().ProductId, ReservationCode = $"RSV-{++opId}-{Guid.NewGuid():N}",
                Quantity = items.Sum(i => i.Quantity), Status = 2,
                ExpiresAt = now.AddDays(7), Reference = $"Order {order.OrderNumber}",
                CreatedAt = now, UpdatedAt = now,
            };
            context.InventoryReservations.Add(reservation);
            await context.SaveChangesAsync(ct);

            foreach (var item in items)
            {
                context.OrderFulfillmentAllocations.Add(new OrderFulfillmentAllocationRecord
                {
                    OrderId = order.Id, OrderItemId = item.Id, CompanyId = order.CompanyId,
                    WarehouseId = wh.Id, ProductId = item.ProductId, Quantity = item.Quantity,
                    ReservationId = reservation.Id, Status = 2, CreatedAt = now, UpdatedAt = now,
                });
            }
        }

        context.Reports.AddRange(Enumerable.Range(0, 3).Select(i => new ReportRecord
        {
            ReporterUserId = buyers[i].Id.ToString(), TargetType = 1,
            TargetId = products[i].Id.ToString(), Reason = 2,
            Description = "Підозріла ціна на товар", Status = 1, Priority = 2,
            CreatedAt = now.AddDays(-rng.Next(5, 15)), UpdatedAt = now,
        }));

        await context.SaveChangesAsync(ct);
    }
}
