using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Domain.Shipping.Repositories;

public interface IShippingQuoteRepository
{
    Task<ShippingQuote?> GetByIdAsync(ShippingQuoteId id, CancellationToken ct = default);
    Task<ShippingQuote> AddAsync(ShippingQuote entity, CancellationToken ct = default);
}
