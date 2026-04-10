using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Repositories;

public interface IProductImageRepository
{
    Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(ProductId productId, CancellationToken ct = default);
    Task ReplaceForProductAsync(ProductId productId, IReadOnlyList<ProductImage> images, CancellationToken ct = default);
}
