namespace Marketplace.Application.Carts.Cache;

public static class CartCacheKeys
{
    public const string ActiveByUserPrefix = "cart:user:";

    public static string ActiveByUser(Guid userId) => $"{ActiveByUserPrefix}{userId}:active";
}
