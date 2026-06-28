using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Domain.Companies.Repositories;

public interface ICompanyLegalProfileRepository
{
    Task<CompanyLegalProfile?> GetByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);
    Task AddAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default);
    Task UpdateAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default);
}
