using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Enums;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ProductFacetSourceRepository : IProductFacetSourceRepository
{
    private readonly ApplicationDbContext _context;

    public ProductFacetSourceRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductFacetSourceRow>> ListActiveFacetSourcesAsync(
        IReadOnlyList<long>? categoryIds = null,
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = _context.Products.AsNoTracking()
            .Where(x => x.Status == (short)ProductStatus.Active && !x.IsDeleted);

        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId == companyId.Value);

        if (categoryIds is { Count: > 0 })
        {
            var categorySet = categoryIds.ToHashSet();
            query = query.Where(x => categorySet.Contains(x.CategoryId));
        }

        var rows = await query
            .GroupJoin(
                _context.ProductDetails.AsNoTracking().Where(x => !x.IsDeleted),
                product => product.Id,
                detail => detail.ProductId,
                (product, details) => new { product, detail = details.FirstOrDefault() })
            .Select(x => new ProductFacetSourceRow(
                x.product.Id,
                x.detail != null ? x.detail.AttributesRaw : null,
                x.detail != null ? x.detail.Tags : Array.Empty<string>(),
                x.detail != null ? x.detail.Brands : Array.Empty<string>()))
            .ToListAsync(ct);

        return rows;
    }
}
