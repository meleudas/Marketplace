using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Inventory.Services;

public interface ICheckoutInventoryService
{
    Task ReserveForOrderAsync(
        OrderId orderId,
        CompanyId companyId,
        IReadOnlyList<(OrderItemId OrderItemId, ProductId ProductId, int Quantity)> lines,
        CancellationToken ct = default);

    Task ConfirmForOrderAsync(OrderId orderId, CompanyId companyId, CancellationToken ct = default);

    Task ReleaseForOrderAsync(OrderId orderId, CompanyId companyId, Guid? actorUserId, string reason, CancellationToken ct = default);
}
