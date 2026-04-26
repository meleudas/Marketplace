using Marketplace.Application.Orders.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(
    long OrderId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<OrderDetailsDto>>;
