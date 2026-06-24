using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Products.Ports;

public interface IProductSimilarityService
{
    Task<Result<SimilarProductsResultDto>> GetSimilarProductsAsync(
        long productId,
        long categoryId,
        string name,
        string description,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> brands,
        decimal price,
        int limit,
        CancellationToken ct = default);
}
