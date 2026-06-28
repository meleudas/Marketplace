using Marketplace.Application.Carts.Ports;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Application.Carts.Services;

public sealed class CartStockWatchSyncService : ICartStockWatchSyncService
{
    private readonly ICartItemRepository _cartItems;
    private readonly ICartStockWatchRepository _watches;
    private readonly IProductRepository _products;
    private readonly IWarehouseStockRepository _stocks;

    public CartStockWatchSyncService(
        ICartItemRepository cartItems,
        ICartStockWatchRepository watches,
        IProductRepository products,
        IWarehouseStockRepository stocks)
    {
        _cartItems = cartItems;
        _watches = watches;
        _products = products;
        _stocks = stocks;
    }

    public async Task SyncWatchForUserCartProductAsync(Guid userId, CartId cartId, ProductId productId, CancellationToken ct = default)
    {
        var items = await _cartItems.ListByCartIdAsync(cartId, ct);
        var cartQty = items
            .Where(i => !i.IsDeleted && i.ProductId == productId)
            .Sum(i => i.Quantity);

        if (cartQty <= 0)
        {
            await _watches.DeleteAsync(userId, productId.Value, ct);
            return;
        }

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null || product.IsDeleted)
        {
            await _watches.DeleteAsync(userId, productId.Value, ct);
            return;
        }

        var stockRows = await _stocks.ListByProductAsync(product.CompanyId, productId, ct);
        var available = stockRows.Sum(s => s.Available);

        if (cartQty > available)
            await _watches.UpsertAsync(userId, productId.Value, ct);
        else
            await _watches.DeleteAsync(userId, productId.Value, ct);
    }
}
