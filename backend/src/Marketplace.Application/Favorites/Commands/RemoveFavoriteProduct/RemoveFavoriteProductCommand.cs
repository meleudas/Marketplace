using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;

public sealed record RemoveFavoriteProductCommand(
    Guid ActorUserId,
    long ProductId) : IRequest<Result<bool>>;
