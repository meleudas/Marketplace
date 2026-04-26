using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System.Text.Json;

namespace Marketplace.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderAccessService _access;
    private readonly IOrderCacheInvalidationService _cacheInvalidation;
    private readonly IOutboxWriter _outbox;
    private readonly IOrderStatusHistoryWriter _historyWriter;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderAccessService access,
        IOrderCacheInvalidationService cacheInvalidation,
        IOutboxWriter outbox,
        IOrderStatusHistoryWriter historyWriter)
    {
        _orderRepository = orderRepository;
        _access = access;
        _cacheInvalidation = cacheInvalidation;
        _outbox = outbox;
        _historyWriter = historyWriter;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result.Failure("Order not found");

        var allowed = await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, OrderPermission.Cancel, ct);
        if (!allowed)
            return Result.Failure("Forbidden");

        try
        {
            var oldStatus = order.Status;
            order.Cancel();
            await _orderRepository.UpdateAsync(order, ct);
            await _historyWriter.WriteIfChangedAsync(
                order,
                oldStatus,
                request.ActorUserId,
                "manual",
                correlationId: null,
                ct: ct);
            await _outbox.AppendAsync(
                "Order",
                order.Id.Value.ToString(),
                "OrderCancelled",
                JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    orderId = order.Id.Value,
                    customerId = order.CustomerId,
                    companyId = order.CompanyId.Value
                }),
                ct);
            await _cacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
