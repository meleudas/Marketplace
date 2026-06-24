using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Services;
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
    private readonly IShipmentFulfillmentService _fulfillment;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IAppNotificationScheduler _appNotifications;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IOrderAccessService access,
        IShipmentFulfillmentService fulfillment,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderStatusHistoryWriter historyWriter,
        IAppNotificationScheduler appNotifications)
    {
        _orderRepository = orderRepository;
        _access = access;
        _fulfillment = fulfillment;
        _orderMutationCoordinator = orderMutationCoordinator;
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
            var shipmentBackedStatusChange = false;
            switch (request.NewStatus)
            {
                case OrderStatus.Processing:
                    order.SetProcessing();
                    break;
                case OrderStatus.Shipped:
                {
                    var readiness = await _fulfillment.BuildReadinessDtoAsync(order.Id, ct);
                    if (!readiness.IsFullyShipped)
                    {
                        var warehouseGroups = readiness.PendingByWarehouse.Count > 0
                            ? readiness.PendingByWarehouse
                            : [new WarehouseFulfillmentGroupDto(0, "Default", readiness.PendingItems)];

                        foreach (var group in warehouseGroups)
                        {
                            if (group.Items.Count == 0)
                                continue;

                            var lines = group.Items
                                .Select(x => new CreateShipmentLineRequest(x.OrderItemId, x.RemainingQuantity))
                                .ToList();
                            var warehouseId = group.WarehouseId > 0 ? WarehouseId.From(group.WarehouseId) : null;
                            var shipmentResult = await _fulfillment.CreateShipmentAsync(
                                order, warehouseId, lines, request.TrackingNumber, request.ActorUserId, ct);
                            if (!shipmentResult.IsSuccess)
                                return Result.Failure(shipmentResult.Error ?? "Failed to create shipment");
                        }

                        var reloaded = await _orderRepository.GetByIdAsync(order.Id, ct);
                        if (reloaded is not null)
                        {
                            shipmentBackedStatusChange = reloaded.Status != oldStatus;
                            order = reloaded;
                        }
                    }
                    else
                    {
                        order.SetShipped(request.TrackingNumber);
                    }
                    break;
                }
                case OrderStatus.Delivered:
                    order.SetDelivered();
                    break;
                default:
                    return Result.Failure("Unsupported target status");
            }

            await _orderRepository.UpdateAsync(order, ct);
            if (!shipmentBackedStatusChange)
            {
                await _historyWriter.WriteIfChangedAsync(
                    order,
                    oldStatus,
                    request.ActorUserId,
                    "manual",
                    correlationId: null,
                    ct: ct);
            }
            await _orderMutationCoordinator.PublishOrderChangedAsync(
                order,
                "OrderStatusChanged",
                $"{request.NewStatus}:{request.ActorUserId}",
                ct);

            if (order.Status is OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            {
                var channels = AppNotificationChannelKind.Push
                    | AppNotificationChannelKind.InApp
                    | AppNotificationChannelKind.Email
                    | AppNotificationChannelKind.Telegram;
                if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
                    channels |= AppNotificationChannelKind.Sms;

                await _appNotifications.ScheduleAsync(
                    new AppNotificationRequest
                    {
                        TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                        CorrelationId = Guid.NewGuid(),
                        Channels = channels,
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
