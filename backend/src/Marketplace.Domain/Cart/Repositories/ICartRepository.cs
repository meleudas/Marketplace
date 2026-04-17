using Marketplace.Domain.Common.ValueObjects;
using DomainCart = Marketplace.Domain.Cart.Entities.Cart;

namespace Marketplace.Domain.Cart.Repositories;

public interface ICartRepository
{
    Task<DomainCart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<DomainCart?> GetByIdAsync(CartId id, CancellationToken ct = default);
    Task<DomainCart> AddAsync(DomainCart cart, CancellationToken ct = default);
    Task UpdateAsync(DomainCart cart, CancellationToken ct = default);
}
