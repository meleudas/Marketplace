namespace Marketplace.Application.Products.Options;

public sealed class SimilarProductsOptions
{
    public const string SectionName = "SimilarProducts";

    public int DefaultLimit { get; set; } = 12;
    public int MaxLimit { get; set; } = 24;
    public int PriceBandPercent { get; set; } = 30;
}
