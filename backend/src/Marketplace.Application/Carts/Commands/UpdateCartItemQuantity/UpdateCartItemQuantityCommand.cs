using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.UpdateCartItemQuantity;

public sealed record UpdateCartItemQuantityCommand(
    Guid ActorUserId,
    long CartItemId,
    int Quantity) : IRequest<Result<CartDto>>;
