using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(
    long OrderId,
    Guid ActorUserId,
    bool IsActorAdmin,
    OrderStatus NewStatus,
    string? TrackingNumber) : IRequest<Result>;
