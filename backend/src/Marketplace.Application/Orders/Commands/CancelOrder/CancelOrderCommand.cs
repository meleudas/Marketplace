using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(
    long OrderId,
    Guid ActorUserId,
    bool IsActorAdmin,
    OrderCancellationReasonCode ReasonCode,
    string? Comment,
    string IdempotencyKey) : IRequest<Result>;
