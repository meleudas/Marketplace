using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;

namespace Marketplace.Domain.Reviews.Repositories;

public interface IReviewReplyRepository
{
    Task<ReviewReply?> GetByProductReviewIdAsync(ProductReviewId reviewId, CancellationToken ct = default);
    Task<ReviewReply?> GetByCompanyReviewIdAsync(CompanyReviewId reviewId, CancellationToken ct = default);
    Task<ReviewReply> AddAsync(ReviewReply reply, CancellationToken ct = default);
    Task UpdateAsync(ReviewReply reply, CancellationToken ct = default);
}
