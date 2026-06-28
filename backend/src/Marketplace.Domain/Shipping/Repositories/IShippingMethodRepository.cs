using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShippingMethodRepository
{
    Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken ct = default);
    Task<IReadOnlyList<ShippingMethod>> ListActiveAsync(CancellationToken ct = default);
}
