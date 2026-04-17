using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
    {
        var row = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
    {
        var row = await _context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.Slug == slug, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var row = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        var values = ids.Select(x => x.Value).ToHashSet();
        var rows = await _context.Products.AsNoTracking()
            .Where(x => values.Contains(x.Id))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.Products.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default)
    {
        var rows = await _context.Products.AsNoTracking()
            .Where(x => x.Status == (short)ProductStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(ToRecord(product), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        var row = await _context.Products.FirstOrDefaultAsync(x => x.Id == product.Id.Value, ct)
            ?? throw new InvalidOperationException("Product not found");

        row.Name = product.Name;
        row.Slug = product.Slug;
        row.Description = product.Description;
        row.Price = product.Price.Amount;
        row.OldPrice = product.OldPrice?.Amount;
        row.Stock = product.Stock;
        row.MinStock = product.MinStock;
        row.CategoryId = product.CategoryId.Value;
        row.Status = (short)product.Status;
        row.Rating = product.Rating;
        row.ReviewCount = product.ReviewCount;
        row.ViewCount = product.ViewCount;
        row.SalesCount = product.SalesCount;
        row.HasVariants = product.HasVariants;
        row.UpdatedAt = product.UpdatedAt;
        row.IsDeleted = product.IsDeleted;
        row.DeletedAt = product.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    private static Product ToDomain(ProductRecord r) =>
        Product.Reconstitute(
            ProductId.From(r.Id),
            CompanyId.From(r.CompanyId),
            r.Name,
            r.Slug,
            r.Description,
            new Money(r.Price),
            r.OldPrice.HasValue ? new Money(r.OldPrice.Value) : null,
            r.Stock,
            r.MinStock,
            CategoryId.From(r.CategoryId),
            (ProductStatus)r.Status,
            r.Rating,
            r.ReviewCount,
            r.ViewCount,
            r.SalesCount,
            r.HasVariants,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static ProductRecord ToRecord(Product p) =>
        new()
        {
            Id = p.Id.Value,
            CompanyId = p.CompanyId.Value,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            Price = p.Price.Amount,
            OldPrice = p.OldPrice?.Amount,
            Stock = p.Stock,
            MinStock = p.MinStock,
            CategoryId = p.CategoryId.Value,
            Status = (short)p.Status,
            Rating = p.Rating,
            ReviewCount = p.ReviewCount,
            ViewCount = p.ViewCount,
            SalesCount = p.SalesCount,
            HasVariants = p.HasVariants,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            IsDeleted = p.IsDeleted,
            DeletedAt = p.DeletedAt
        };
}
