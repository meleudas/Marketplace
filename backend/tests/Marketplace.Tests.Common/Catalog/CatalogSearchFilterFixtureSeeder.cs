using System.Text.Json;
using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Tests.Common.Catalog;

public static class CatalogSearchFilterFixtureSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<IProductSearchIndexer>();

        if (await db.Products.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var companyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        db.Companies.Add(new CompanyRecord
        {
            Id = companyId,
            Name = "Catalog Search Publisher",
            Slug = "catalog-search-publisher",
            Description = "Catalog search fixture publisher",
            ContactEmail = "catalog-search@test.local",
            ContactPhone = "+380000000000",
            AddressStreet = "Test",
            AddressCity = "Kyiv",
            AddressState = "Kyiv",
            AddressPostalCode = "01001",
            AddressCountry = "UA",
            IsApproved = true,
            ApprovedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        });

        db.Categories.AddRange(
            Category(1, "Fiction", "fiction", null, now),
            Category(2, "Documentary", "documentary", null, now),
            Category(13, "Fantasy", "fiction-fantasy", 1, now),
            Category(21, "Biographies", "documentary-biographies", 2, now));

        db.Warehouses.Add(new WarehouseRecord
        {
            Id = 1,
            CompanyId = companyId,
            Name = "Catalog Search Warehouse",
            Code = "catalog-search-wh",
            Street = "Test",
            City = "Kyiv",
            State = "Kyiv",
            PostalCode = "01001",
            Country = "UA",
            Priority = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        });

        long productId = 7001;
        long detailId = 7001;
        long stockId = 7001;

        foreach (var fixture in CatalogSearchFilterOracle.ContainerProducts)
        {
            db.Products.Add(new ProductRecord
            {
                Id = productId,
                CompanyId = companyId,
                Name = fixture.Name,
                Slug = fixture.Slug,
                Description = fixture.Name,
                Price = fixture.Price,
                Stock = 0,
                MinStock = 5,
                CategoryId = fixture.CategoryId,
                Status = 1,
                HasVariants = false,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            });

            db.ProductDetails.Add(new ProductDetailRecord
            {
                Id = detailId,
                ProductId = productId,
                Slug = fixture.Slug,
                AttributesRaw = JsonSerializer.Serialize(new
                {
                    author = fixture.Author,
                    genre = fixture.Genre,
                    format = fixture.Format,
                }),
                Tags =
                [
                    fixture.Genre,
                    $"format:{fixture.Format}",
                ],
                Brands = [fixture.Author],
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            });

            var onHand = string.Equals(fixture.AvailabilityStatus, "low_stock", StringComparison.OrdinalIgnoreCase) ? 3 : 20;
            db.WarehouseStocks.Add(new WarehouseStockRecord
            {
                Id = stockId,
                CompanyId = companyId,
                WarehouseId = 1,
                ProductId = productId,
                OnHand = onHand,
                Reserved = 0,
                ReorderPoint = 1,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            });

            productId++;
            detailId++;
            stockId++;
        }

        await db.SaveChangesAsync(ct);
        await indexer.FullReindexAsync(ct);
    }

    private static CategoryRecord Category(long id, string name, string slug, long? parentId, DateTime now) =>
        new()
        {
            Id = id,
            Name = name,
            Slug = slug,
            ParentId = parentId,
            Description = name,
            SortOrder = (int)id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
}
