namespace Marketplace.Application.Favorites.Cache;

public static class FavoritesCacheKeys
{
    public const string ListByUserPrefix = "favorites:user:";

    public static string ListByUser(Guid userId) => $"{ListByUserPrefix}{userId}:list";
}
