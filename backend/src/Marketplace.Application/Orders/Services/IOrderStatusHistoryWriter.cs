using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Orders.Services;

public interface IOrderStatusHistoryWriter
{
    Task RecordCreatedAsync(
        Order order,
        Guid actorUserId,
        string source,
        string? correlationId,
        CancellationToken ct = default);

    Task WriteIfChangedAsync(
        Order order,
        Marketplace.Domain.Orders.Enums.OrderStatus oldStatus,
        Guid actorUserId,
        string source,
        string? comment = null,
        string? correlationId = null,
        CancellationToken ct = default);
}
