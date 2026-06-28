using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IUserAddressRepository
{
    Task<UserAddress?> GetByIdAsync(UserAddressId id, CancellationToken ct = default);
    Task<IReadOnlyList<UserAddress>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserAddress> AddAsync(UserAddress entity, CancellationToken ct = default);
    Task UpdateAsync(UserAddress entity, CancellationToken ct = default);
    Task SoftDeleteAsync(UserAddressId id, DateTime deletedAtUtc, CancellationToken ct = default);
    Task ClearDefaultAsync(Guid userId, CancellationToken ct = default);
}
