using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Enums;

namespace Marketplace.Domain.Reviews.Repositories;

public interface IProductReviewRepository
{
    Task<ProductReview?> GetByIdAsync(ProductReviewId id, CancellationToken ct = default);
    Task<ProductReview?> GetByProductAndUserAsync(ProductId productId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductReview>> ListByProductAsync(ProductId productId, int page, int size, CancellationToken ct = default);
    Task<(decimal? Average, int Count)> GetApprovedStatsAsync(ProductId productId, CancellationToken ct = default);
    Task<ProductReview> AddAsync(ProductReview review, CancellationToken ct = default);
    Task UpdateAsync(ProductReview review, CancellationToken ct = default);
    Task SoftDeleteAsync(ProductReviewId id, DateTime utcNow, CancellationToken ct = default);
    Task<IReadOnlyList<ProductReview>> ListByStatusAsync(ReviewModerationStatus status, int page, int size, CancellationToken ct = default);
}
