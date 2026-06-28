using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Entities;

namespace Marketplace.Domain.Returns.Repositories;

public interface IReturnLineItemRepository
{
    Task<IReadOnlyList<ReturnLineItem>> ListByReturnRequestIdAsync(ReturnRequestId returnRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<ReturnLineItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<ReturnLineItem> items, CancellationToken ct = default);
}
