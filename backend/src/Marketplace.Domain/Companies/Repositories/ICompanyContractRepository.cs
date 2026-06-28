using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Domain.Companies.Repositories;

public interface ICompanyContractRepository
{
    Task<CompanyContract?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);
    Task<CompanyContract?> GetByIdAsync(CompanyContractId id, CancellationToken ct = default);
    Task<CompanyContract> AddAsync(CompanyContract contract, CancellationToken ct = default);
}
