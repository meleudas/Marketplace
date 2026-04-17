using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(
    Guid ActorUserId,
    long CartItemId) : IRequest<Result<CartDto>>;
