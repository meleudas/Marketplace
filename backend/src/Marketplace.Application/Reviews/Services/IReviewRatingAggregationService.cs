using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Reviews.Services;

public interface IReviewRatingAggregationService
{
    Task RecalculateProductAsync(ProductId productId, CancellationToken ct = default);
    Task RecalculateCompanyAsync(CompanyId companyId, CancellationToken ct = default);
}
