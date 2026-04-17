using Marketplace.Application.Favorites.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Favorites.Commands.AddFavoriteProduct;

public sealed record AddFavoriteProductCommand(
    Guid ActorUserId,
    long ProductId) : IRequest<Result<FavoriteItemDto>>;
