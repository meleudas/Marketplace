using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Domain.Companies.Repositories;

public interface ICompanyCommissionRateRepository
{
    Task<CompanyCommissionRate?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);

    Task<CompanyCommissionRate?> GetActiveAtAsync(CompanyId companyId, DateTime asOfUtc, CancellationToken ct = default);

    Task<IReadOnlyList<CompanyCommissionRate>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);

    Task<CompanyCommissionRate> AddAsync(CompanyCommissionRate rate, CancellationToken ct = default);
    Task UpdateAsync(CompanyCommissionRate rate, CancellationToken ct = default);
}
