using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Policies;
using Marketplace.Application.Orders.Services;
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
    private readonly OrderCancellationPolicy _cancellationPolicy;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IAppNotificationScheduler _appNotifications;
    private readonly ICheckoutInventoryService _checkoutInventory;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderAccessService access,
        OrderCancellationPolicy cancellationPolicy,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderStatusHistoryWriter historyWriter,
        IAppNotificationScheduler appNotifications,
        ICheckoutInventoryService checkoutInventory)
    {
        _orderRepository = orderRepository;
        _access = access;
        _cancellationPolicy = cancellationPolicy;
        _orderMutationCoordinator = orderMutationCoordinator;
        _historyWriter = historyWriter;
        _appNotifications = appNotifications;
        _checkoutInventory = checkoutInventory;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result.Failure("Order not found");

        var allowed = await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, OrderPermission.Cancel, ct);
        if (!allowed)
            return Result.Failure("Forbidden");

        var actor = await _access.ResolveCancellationActorAsync(order, request.ActorUserId, request.IsActorAdmin, ct);
        var policyResult = _cancellationPolicy.Validate(
            order,
            actor,
            request.ReasonCode,
            request.Comment,
            DateTime.UtcNow);
        if (!policyResult.IsSuccess)
            return policyResult;

        try
        {
            var oldStatus = order.Status;
            var adminOverride = actor == OrderCancellationActor.Admin
                && oldStatus is Marketplace.Domain.Orders.Enums.OrderStatus.Shipped
                    or Marketplace.Domain.Orders.Enums.OrderStatus.Delivered;
            order.Cancel(request.ReasonCode, request.Comment, adminOverride);
            await _orderRepository.UpdateAsync(order, ct);

            var historyComment = string.IsNullOrWhiteSpace(request.Comment)
                ? request.ReasonCode.ToString()
                : $"{request.ReasonCode}: {request.Comment}";
            await _historyWriter.WriteIfChangedAsync(
                order,
                oldStatus,
                request.ActorUserId,
                "cancel",
                comment: historyComment,
                correlationId: null,
                ct: ct);
            await _orderMutationCoordinator.PublishOrderChangedAsync(
                order,
                "OrderCancelled",
                $"cancel:{request.ActorUserId}",
                ct);
            await _checkoutInventory.ReleaseForOrderAsync(
                order.Id,
                order.CompanyId,
                request.ActorUserId,
                "order-cancelled",
                ct);

            await _appNotifications.ScheduleAsync(
                new AppNotificationRequest
                {
                    TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                    CorrelationId = Guid.NewGuid(),
                    Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
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

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
