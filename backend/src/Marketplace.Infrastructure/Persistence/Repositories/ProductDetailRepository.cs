using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ProductDetailRepository : IProductDetailRepository
{
    private readonly ApplicationDbContext _context;

    public ProductDetailRepository(ApplicationDbContext context) => _context = context;

    public async Task<ProductDetail?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
    {
        var row = await _context.ProductDetails.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task AddAsync(ProductDetail detail, CancellationToken ct = default)
    {
        await _context.ProductDetails.AddAsync(ToRecord(detail), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ProductDetail detail, CancellationToken ct = default)
    {
        var row = await _context.ProductDetails.FirstOrDefaultAsync(x => x.Id == detail.Id.Value, ct)
            ?? throw new InvalidOperationException("Product detail not found");

        row.Slug = detail.Slug;
        row.AttributesRaw = detail.Attributes.Raw;
        row.VariantsRaw = detail.Variants.Raw;
        row.SpecificationsRaw = detail.Specifications.Raw;
        row.SeoRaw = detail.Seo.Raw;
        row.ContentBlocksRaw = detail.ContentBlocks.Raw;
        row.Tags = detail.Tags.ToArray();
        row.Brands = detail.Brands.ToArray();
        row.UpdatedAt = detail.UpdatedAt;
        row.IsDeleted = detail.IsDeleted;
        row.DeletedAt = detail.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static ProductDetail ToDomain(ProductDetailRecord r) =>
        ProductDetail.Reconstitute(
            ProductDetailId.From(r.Id),
            ProductId.From(r.ProductId),
            r.Slug,
            new JsonBlob(r.AttributesRaw),
            new JsonBlob(r.VariantsRaw),
            new JsonBlob(r.SpecificationsRaw),
            new JsonBlob(r.SeoRaw),
            new JsonBlob(r.ContentBlocksRaw),
            r.Tags,
            r.Brands,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static ProductDetailRecord ToRecord(ProductDetail d) =>
        new()
        {
            Id = d.Id.Value,
            ProductId = d.ProductId.Value,
            Slug = d.Slug,
            AttributesRaw = d.Attributes.Raw,
            VariantsRaw = d.Variants.Raw,
            SpecificationsRaw = d.Specifications.Raw,
            SeoRaw = d.Seo.Raw,
            ContentBlocksRaw = d.ContentBlocks.Raw,
            Tags = d.Tags.ToArray(),
            Brands = d.Brands.ToArray(),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            IsDeleted = d.IsDeleted,
            DeletedAt = d.DeletedAt
        };
}
