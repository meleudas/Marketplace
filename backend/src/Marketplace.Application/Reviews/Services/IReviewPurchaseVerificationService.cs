using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Reviews.Services;

public interface IReviewPurchaseVerificationService
{
    Task<long?> GetVerifiedProductOrderIdAsync(Guid userId, ProductId productId, CancellationToken ct = default);
    Task<long?> GetVerifiedCompanyOrderIdAsync(Guid userId, CompanyId companyId, CancellationToken ct = default);
}
