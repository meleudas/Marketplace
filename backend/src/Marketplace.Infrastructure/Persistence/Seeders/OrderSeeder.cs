using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class OrderSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Orders.AnyAsync(ct))
            return;

        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var buyers = await context.MarketplaceUsers.Where(u => u.Role == 1).ToListAsync(ct);
        var companies = await context.Companies.ToListAsync(ct);
        var products = await context.Products.ToListAsync(ct);
        var shipping = await context.ShippingMethods.ToListAsync(ct);
        var warehouses = await context.Warehouses.ToListAsync(ct);
        var stock = await context.WarehouseStocks.ToListAsync(ct);
        var whByProd = stock.ToLookup(s => s.ProductId);
        var adminId = (await context.MarketplaceUsers.FirstAsync(u => u.Role == 4, ct)).Id;

        var orderData = new List<(OrderRecord Order, List<OrderItemRecord> Items, decimal Total)>();
        int opId = 0;

        foreach (var buyer in buyers.OrderBy(_ => rng.Next()).Take(30))
        {
            var company = companies[rng.Next(companies.Count)];
            var avail = products.Where(p => p.CompanyId == company.Id).ToList();
            var picked = avail.OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)).ToList();
            if (picked.Count == 0) continue;

            var ship = shipping[rng.Next(shipping.Count)];
            var subtotal = picked.Sum(p => p.Price);
            var shipCost = ship.Price;
            var discount = rng.Next(3) == 0 ? Math.Round(subtotal * 0.1m, 2) : 0;
            var total = Math.Round(subtotal + shipCost - discount, 2);
            var created = now.AddDays(-rng.Next(20, 40));

            var order = new OrderRecord
            {
                OrderNumber = $"ORD-{created:yyyyMMdd}-{rng.Next(1000, 9999)}",
                CustomerId = buyer.Id, CompanyId = company.Id, Status = 4,
                TotalPrice = total, Subtotal = subtotal, ShippingCost = shipCost,
                DiscountAmount = discount, ShippingMethodId = ship.Id, PaymentMethod = 1,
                ShippedAt = created.AddDays(rng.Next(3, 8)),
                DeliveredAt = created.AddDays(rng.Next(8, 15)),
                CreatedAt = created, UpdatedAt = now,
            };

            var items = picked.Select(p => new OrderItemRecord
            {
                ProductId = p.Id, ProductName = p.Name,
                Quantity = rng.Next(1, 3), PriceAtMoment = p.Price,
                TotalPrice = Math.Round(p.Price * (rng.Next(1, 3)), 2),
                CompanyId = company.Id, CreatedAt = created, UpdatedAt = created,
            }).ToList();

            foreach (var item in items)
            {
                foreach (var se in whByProd[item.ProductId])
                {
                    se.OnHand = Math.Max(0, se.OnHand - item.Quantity);
                    se.Reserved = Math.Max(0, se.Reserved - 1);
                }
            }

            orderData.Add((order, items, total));
        }

        context.Orders.AddRange(orderData.Select(d => d.Order));
        await context.SaveChangesAsync(ct);

        var payments = new List<PaymentRecord>();
        var allItems = new List<OrderItemRecord>();
        var hist = new List<OrderStatusHistoryRecord>();
        var addrs = new List<OrderAddressSnapshotRecord>();
        var fin = new List<OrderFinancialsRecord>();
        var movs = new List<StockMovementRecord>();

        foreach (var (order, items, total) in orderData)
        {
            foreach (var item in items)
            {
                item.OrderId = order.Id;
                allItems.Add(item);

                var wh = warehouses.FirstOrDefault(w => w.CompanyId == order.CompanyId);
                if (wh != null)
                    movs.Add(new StockMovementRecord
                    {
                        CompanyId = order.CompanyId, WarehouseId = wh.Id,
                        ProductId = item.ProductId, Type = 2, Quantity = -item.Quantity,
                        OperationId = $"sale-{++opId}-{Guid.NewGuid():N}",
                        Reference = $"Order {order.OrderNumber}", Reason = "Продаж",
                        ActorUserId = order.CustomerId, OccurredAt = order.CreatedAt,
                        CreatedAt = order.CreatedAt, UpdatedAt = order.CreatedAt,
                    });
            }

            var payment = new PaymentRecord
            {
                OrderId = order.Id, PaymentMethod = 1, Amount = total,
                Currency = "UAH", Status = 3, ProcessedAt = order.CreatedAt.AddHours(1),
                CreatedAt = order.CreatedAt, UpdatedAt = order.CreatedAt,
            };
            payments.Add(payment);

            foreach (var (old, New, days) in new[] { (0, 1, 0), (1, 2, 3), (2, 4, 10) })
                hist.Add(new OrderStatusHistoryRecord
                {
                    OrderId = order.Id, OldStatus = (short)old, NewStatus = (short)New,
                    ChangedByUserId = adminId, Source = "system", ChangedAt = order.CreatedAt.AddDays(days),
                    CreatedAt = order.CreatedAt.AddDays(days), UpdatedAt = order.CreatedAt.AddDays(days),
                });

            addrs.Add(new OrderAddressSnapshotRecord
            {
                OrderId = order.Id, Kind = 1, FirstName = "Олена", LastName = "Ковальчук",
                Phone = "+380501234567", Street = "вул. Хрещатик, 1", City = "Київ",
                State = "Київська", PostalCode = "01001", Country = "Україна",
                CreatedAt = order.CreatedAt, UpdatedAt = order.CreatedAt,
            });
        }

        context.OrderItems.AddRange(allItems);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync(ct);

        foreach (var (order, _, _) in orderData)
        {
            var payment = payments.First(p => p.OrderId == order.Id);
            fin.Add(new OrderFinancialsRecord
            {
                OrderId = order.Id, PaymentId = payment.Id, CompanyId = order.CompanyId,
                Currency = "UAH", MerchandiseSubtotal = order.Subtotal,
                DiscountAmount = order.DiscountAmount, MerchandiseBase = order.Subtotal - order.DiscountAmount,
                CommissionPercent = 0.15m, PlatformFee = Math.Round(order.Subtotal * 0.15m, 2),
                SellerMerchandiseNet = Math.Round(order.Subtotal * 0.85m, 2),
                ShippingAmount = order.ShippingCost, SellerPayoutEligible = Math.Round(order.TotalPrice * 0.85m, 2),
                PostedAtUtc = order.DeliveredAt ?? now, CreatedAt = now, UpdatedAt = now,
            });
        }

        context.OrderStatusHistory.AddRange(hist);
        context.OrderAddresses.AddRange(addrs);
        context.OrderFinancials.AddRange(fin);
        context.StockMovements.AddRange(movs);
        await context.SaveChangesAsync(ct);
    }
}
