using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly ApplicationDbContext _context;

    public ProductImageRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(ProductId productId, CancellationToken ct = default)
    {
        var rows = await _context.ProductImages.AsNoTracking()
            .Where(x => x.ProductId == productId.Value)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task ReplaceForProductAsync(ProductId productId, IReadOnlyList<ProductImage> images, CancellationToken ct = default)
    {
        var existing = await _context.ProductImages.Where(x => x.ProductId == productId.Value).ToListAsync(ct);
        _context.ProductImages.RemoveRange(existing);
        if (images.Count > 0)
            await _context.ProductImages.AddRangeAsync(images.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    private static ProductImage ToDomain(ProductImageRecord r) =>
        ProductImage.Reconstitute(
            ProductImageId.From(r.Id),
            ProductId.From(r.ProductId),
            r.ImageUrl,
            r.ThumbnailUrl,
            r.OriginalObjectKey,
            r.ImageObjectKey,
            r.ThumbnailObjectKey,
            r.AltText,
            r.SortOrder,
            r.IsMain,
            r.Width,
            r.Height,
            r.FileSize,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static ProductImageRecord ToRecord(ProductImage x) =>
        new()
        {
            Id = x.Id.Value,
            ProductId = x.ProductId.Value,
            ImageUrl = x.ImageUrl,
            ThumbnailUrl = x.ThumbnailUrl,
            OriginalObjectKey = x.OriginalObjectKey,
            ImageObjectKey = x.ImageObjectKey,
            ThumbnailObjectKey = x.ThumbnailObjectKey,
            AltText = x.AltText,
            SortOrder = x.SortOrder,
            IsMain = x.IsMain,
            Width = x.Width,
            Height = x.Height,
            FileSize = x.FileSize,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
