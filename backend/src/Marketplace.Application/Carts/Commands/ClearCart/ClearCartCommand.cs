using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.ClearCart;

public sealed record ClearCartCommand(Guid ActorUserId) : IRequest<Result<CartDto>>;
