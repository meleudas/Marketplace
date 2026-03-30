using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.ValueObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Marketplace.Domain.Users.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
        Task<User?> GetByIdentityIdAsync(IdentityUserId identityId, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<User>> SearchByUserNameAsync(string userName, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
    }
}
