using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Carts.Services;

public interface ICartStockWatchSyncService
{
    /// <summary>After cart lines change: maintain watch when cart quantity exceeds aggregate available stock.</summary>
    Task SyncWatchForUserCartProductAsync(Guid userId, CartId cartId, ProductId productId, CancellationToken ct = default);
}
