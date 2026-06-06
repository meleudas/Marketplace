using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShippingEventRepository
{
    Task<bool> ExistsByDedupAsync(ShippingCarrierCode carrierCode, string eventKey, string payloadHash, CancellationToken ct = default);
    Task<ShippingEvent> AddAsync(ShippingEvent entity, CancellationToken ct = default);
}
