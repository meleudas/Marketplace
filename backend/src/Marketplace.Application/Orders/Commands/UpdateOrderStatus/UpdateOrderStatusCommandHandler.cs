using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System.Text.Json;

namespace Marketplace.Application.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderAccessService _access;
    private readonly IOrderCacheInvalidationService _cacheInvalidation;
    private readonly IOutboxWriter _outbox;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IAppNotificationScheduler _appNotifications;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IOrderAccessService access,
        IOrderCacheInvalidationService cacheInvalidation,
        IOutboxWriter outbox,
        IOrderStatusHistoryWriter historyWriter,
        IAppNotificationScheduler appNotifications)
    {
        _orderRepository = orderRepository;
        _access = access;
        _cacheInvalidation = cacheInvalidation;
        _outbox = outbox;
        _historyWriter = historyWriter;
        _appNotifications = appNotifications;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result.Failure("Order not found");

        var allowed = await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, OrderPermission.ManageStatus, ct);
        if (!allowed)
            return Result.Failure("Forbidden");

        try
        {
            var oldStatus = order.Status;
            switch (request.NewStatus)
            {
                case OrderStatus.Processing:
                    order.SetProcessing();
                    break;
                case OrderStatus.Shipped:
                    order.SetShipped(request.TrackingNumber);
                    break;
                case OrderStatus.Delivered:
                    order.SetDelivered();
                    break;
                default:
                    return Result.Failure("Unsupported target status");
            }

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
                "OrderStatusChanged",
                JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    orderId = order.Id.Value,
                    customerId = order.CustomerId,
                    companyId = order.CompanyId.Value,
                    status = order.Status.ToString()
                }),
                ct);
            await _cacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);

            if (order.Status is OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            {
                await _appNotifications.ScheduleAsync(
                    new AppNotificationRequest
                    {
                        TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                        CorrelationId = Guid.NewGuid(),
                        Channels = AppNotificationChannelKind.Push
                            | AppNotificationChannelKind.InApp
                            | AppNotificationChannelKind.Email
                            | AppNotificationChannelKind.Telegram,
                        Audience = AppNotificationAudienceKind.User,
                        TargetUserId = order.CustomerId,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            orderId = order.Id.Value,
                            orderNumber = order.OrderNumber,
                            status = order.Status.ToString()
                        })
                    },
                    ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
