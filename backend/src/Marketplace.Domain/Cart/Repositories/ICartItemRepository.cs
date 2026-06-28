using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Cart.Repositories;

public interface ICartItemRepository
{
    Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default);
    Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default);
    Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default);
    Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default);
    Task UpdateAsync(CartItem item, CancellationToken ct = default);
    Task SoftDeleteAsync(CartItemId id, DateTime utcNow, CancellationToken ct = default);
    Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default);
}
