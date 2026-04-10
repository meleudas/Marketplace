using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Repositories;

public interface IProductDetailRepository
{
    Task<ProductDetail?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);
    Task AddAsync(ProductDetail detail, CancellationToken ct = default);
    Task UpdateAsync(ProductDetail detail, CancellationToken ct = default);
}
