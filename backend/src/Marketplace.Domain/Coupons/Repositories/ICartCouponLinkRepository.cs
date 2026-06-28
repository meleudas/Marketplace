using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;

namespace Marketplace.Domain.Coupons.Repositories;

public interface ICartCouponLinkRepository
{
    Task<CartCouponLink?> GetByCartIdAsync(CartId cartId, CancellationToken ct = default);
    Task<CartCouponLink> UpsertAsync(CartCouponLink entity, CancellationToken ct = default);
    Task RemoveByCartIdAsync(CartId cartId, CancellationToken ct = default);
}
