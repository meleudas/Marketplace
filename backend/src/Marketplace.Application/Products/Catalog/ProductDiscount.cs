namespace Marketplace.Application.Products.Catalog;

public static class ProductDiscount
{
    public static bool IsOnSale(decimal price, decimal? oldPrice)
        => oldPrice is > 0 and var old && old > price;

    public static decimal? Percent(decimal price, decimal? oldPrice)
    {
        if (!IsOnSale(price, oldPrice))
            return null;

        return Math.Round((oldPrice!.Value - price) / oldPrice.Value * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
