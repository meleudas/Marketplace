using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListActiveOnSaleAsync(
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minDiscountPercent = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListActiveNewestAsync(
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListActivePopularAsync(
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
}
