using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Repositories;

namespace Marketplace.Application.Orders.Services;

public sealed class OrderStatusHistoryWriter : IOrderStatusHistoryWriter
{
    private readonly IOrderStatusHistoryRepository _historyRepository;

    public OrderStatusHistoryWriter(IOrderStatusHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task WriteIfChangedAsync(
        Order order,
        Marketplace.Domain.Orders.Enums.OrderStatus oldStatus,
        Guid actorUserId,
        string source,
        string? comment = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        if (oldStatus == order.Status)
            return;

        var history = OrderStatusHistory.Create(
            OrderStatusHistoryId.From(0),
            order.Id,
            oldStatus,
            order.Status,
            actorUserId,
            source,
            comment,
            correlationId);

        await _historyRepository.AddAsync(history, ct);
    }
}
