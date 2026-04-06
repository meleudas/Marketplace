using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Categories.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default);
        Task<Category> AddAsync(Category category, CancellationToken ct = default);
        Task UpdateAsync(Category category, CancellationToken ct = default);
    }
}
