using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Reviews.Repositories;

public interface ICompanyReviewRepository
{
    Task<CompanyReview?> GetByIdAsync(CompanyReviewId id, CancellationToken ct = default);
    Task<CompanyReview?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyReview>> ListByCompanyAsync(CompanyId companyId, int page, int size, CancellationToken ct = default);
    Task<(decimal? Average, int Count)> GetApprovedStatsAsync(CompanyId companyId, CancellationToken ct = default);
    Task<CompanyReview> AddAsync(CompanyReview review, CancellationToken ct = default);
    Task UpdateAsync(CompanyReview review, CancellationToken ct = default);
    Task SoftDeleteAsync(CompanyReviewId id, DateTime utcNow, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyReview>> ListByStatusAsync(CompanyReviewStatus status, int page, int size, CancellationToken ct = default);
}
