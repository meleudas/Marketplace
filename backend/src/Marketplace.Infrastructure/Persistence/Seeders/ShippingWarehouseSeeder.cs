using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class ShippingWarehouseSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        var shippingSeeded = await context.ShippingMethods.AnyAsync(ct);
        if (!shippingSeeded)
        {
            context.ShippingMethods.AddRange(new[]
            {
                new ShippingMethodRecord { Name = "Нова Пошта", Code = 1, Price = 80, FreeShippingThreshold = 1000, EstimatedDaysMin = 1, EstimatedDaysMax = 3, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new ShippingMethodRecord { Name = "Укрпошта", Code = 2, Price = 40, FreeShippingThreshold = 1500, EstimatedDaysMin = 3, EstimatedDaysMax = 7, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new ShippingMethodRecord { Name = "Meest Express", Code = 3, Price = 65, FreeShippingThreshold = 1200, EstimatedDaysMin = 2, EstimatedDaysMax = 4, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new ShippingMethodRecord { Name = "Самовивіз", Code = 4, Price = 0, EstimatedDaysMin = 0, EstimatedDaysMax = 0, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            });
            await context.SaveChangesAsync(ct);
        }

        if (await context.Warehouses.AnyAsync(ct))
            return;

        var companies = await context.Companies.ToListAsync(ct);
        var products = await context.Products.ToListAsync(ct);
        var now = DateTime.UtcNow;
        var rng = Random.Shared;

        var warehouses = companies.Select(c => new WarehouseRecord
        {
            CompanyId = c.Id,
            Name = $"Склад {c.Name}",
            Code = $"WH-{c.Name[..Math.Min(4, c.Name.Length)].ToUpperInvariant()}-{rng.Next(100, 999)}",
            Street = c.AddressStreet,
            City = c.AddressCity,
            State = c.AddressState,
            PostalCode = c.AddressPostalCode,
            Country = c.AddressCountry,
            TimeZone = "Europe/Kyiv",
            Priority = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        context.Warehouses.AddRange(warehouses);
        await context.SaveChangesAsync(ct);

        var warehouseDict = warehouses.ToDictionary(w => w.CompanyId, w => w.Id);
        var stockRecords = new List<WarehouseStockRecord>();
        var movements = new List<StockMovementRecord>();
        var operationId = 0;

        foreach (var product in products)
        {
            if (!warehouseDict.TryGetValue(product.CompanyId, out var whId))
                continue;

            var onHand = rng.Next(10, 150);
            stockRecords.Add(new WarehouseStockRecord
            {
                CompanyId = product.CompanyId,
                WarehouseId = whId,
                ProductId = product.Id,
                OnHand = onHand,
                Reserved = rng.Next(0, 5),
                ReorderPoint = 10,
                CreatedAt = now,
                UpdatedAt = now,
            });

            movements.Add(new StockMovementRecord
            {
                CompanyId = product.CompanyId,
                WarehouseId = whId,
                ProductId = product.Id,
                Type = 1,
                Quantity = onHand,
                OperationId = $"init-{++operationId}-{Guid.NewGuid():N}",
                Reference = "Початкове надходження",
                Reason = "Seed data",
                ActorUserId = companies.First(c => c.Id == product.CompanyId).Id,
                OccurredAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        context.WarehouseStocks.AddRange(stockRecords);
        context.StockMovements.AddRange(movements);
        await context.SaveChangesAsync(ct);
    }
}
