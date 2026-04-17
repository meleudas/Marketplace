using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Companies.Repositories
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct = default);
        Task<Company?> GetApprovedNotDeletedBySlugAsync(string slug, CancellationToken ct = default);
        Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Company>> GetApprovedAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Company>> GetPendingApprovalAsync(CancellationToken ct = default);
        Task AddAsync(Company company, CancellationToken ct = default);
        Task UpdateAsync(Company company, CancellationToken ct = default);
    }
}
