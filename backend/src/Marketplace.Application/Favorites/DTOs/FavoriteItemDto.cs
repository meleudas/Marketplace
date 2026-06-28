namespace Marketplace.Application.Favorites.DTOs;

public sealed record FavoriteItemDto(
    long Id,
    long ProductId,
    DateTime AddedAt,
    decimal? PriceAtAdd,
    bool IsAvailable);
