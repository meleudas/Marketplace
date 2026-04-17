using Marketplace.Application.Favorites.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Favorites.Queries.GetMyFavorites;

public sealed record GetMyFavoritesQuery(Guid ActorUserId) : IRequest<Result<IReadOnlyList<FavoriteItemDto>>>;
