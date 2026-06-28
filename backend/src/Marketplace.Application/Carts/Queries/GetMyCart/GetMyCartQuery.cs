using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Queries.GetMyCart;

public sealed record GetMyCartQuery(Guid ActorUserId) : IRequest<Result<CartDto>>;
