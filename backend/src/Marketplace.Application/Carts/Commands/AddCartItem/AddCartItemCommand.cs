using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.AddCartItem;

public sealed record AddCartItemCommand(
    Guid ActorUserId,
    long ProductId,
    int Quantity) : IRequest<Result<CartDto>>;
