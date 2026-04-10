using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Companies.Repositories;

public interface ICompanyMemberRepository
{
    Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default);
    Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default);
    Task AddAsync(CompanyMember member, CancellationToken ct = default);
    Task UpdateAsync(CompanyMember member, CancellationToken ct = default);
}
