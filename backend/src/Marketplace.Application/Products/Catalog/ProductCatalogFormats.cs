namespace Marketplace.Application.Products.Catalog;

public static class ProductCatalogFormats
{
    public const string Paper = "паперова";
    public const string Electronic = "електронна";

    public static string? Canonicalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ProductCatalogFacetReader.Normalize(value) switch
        {
            Paper => Paper,
            Electronic => Electronic,
            "паперовий" or "paperova" => Paper,
            "електронний" or "elektronna" or "electronic" => Electronic,
            _ => ProductCatalogFacetReader.Normalize(value)
        };
    }

    public static string GetLabel(string canonicalFormat) =>
        canonicalFormat switch
        {
            Paper => "Паперова",
            Electronic => "Електронна",
            _ => canonicalFormat
        };
}
