using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;

namespace Marketplace.Tests.Common.Catalog;

[Flags]
public enum CatalogSearchFilterDimension : byte
{
    None = 0,
    Category = 1,
    Price = 2,
    Author = 4,
    Genre = 8,
    Format = 16,
    Name = 32,
    Availability = 64,
}

public sealed record CatalogSearchFixtureProduct(
    string Slug,
    long CategoryId,
    long RootCategoryId,
    string Author,
    string Genre,
    string Format,
    decimal Price,
    string AvailabilityStatus,
    string Name);

public sealed record CatalogSearchFilterValues(
    IReadOnlyList<long>? CategoryIds,
    long? CategoryRootId,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyList<string>? Authors,
    IReadOnlyList<string>? Genres,
    string? Format,
    string? Name,
    string? AvailabilityStatus);

public sealed record CatalogSearchFilterCase(
    byte Mask,
    SearchCatalogProductsQuery Query,
    IReadOnlyList<string> ExpectedSlugs);

public static class CatalogSearchFilterOracle
{
    public const long FictionRootCategoryId = 1;
    public const long DocumentaryRootCategoryId = 2;

    public static readonly CatalogSearchFixtureProduct[] ContainerProducts =
    [
        new("hobbit-fantasy", 13, FictionRootCategoryId, "Tolkien", "fantasy", ProductCatalogFormats.Paper, 250, "in_stock", "The Hobbit"),
        new("orwell-scifi", 13, FictionRootCategoryId, "Orwell", "sci-fi", ProductCatalogFormats.Paper, 450, "in_stock", "1984"),
        new("shevchenko-memoir", 21, DocumentaryRootCategoryId, "Shevchenko", "memoir", ProductCatalogFormats.Electronic, 150, "in_stock", "Kobzar Documentary"),
        new("tolkien-low-stock", 13, FictionRootCategoryId, "Martin", "fantasy", ProductCatalogFormats.Paper, 180, "low_stock", "Tolkien Low Stock"),
    ];

    public static readonly CatalogSearchFixtureProduct[] SeedAnchorProducts =
    [
        new("seed-book-001", 14, FictionRootCategoryId, "Тарас Шевченко", "", "", 349, "in_stock", "Кобзар"),
        new("seed-book-002", 14, FictionRootCategoryId, "Леся Українка", "", "", 299, "in_stock", "Лісова пісня"),
        new("seed-book-003", 11, FictionRootCategoryId, "Михайло Коцюбинський", "", "", 279, "in_stock", "Тіні забутих предків"),
        new("seed-book-004", 16, FictionRootCategoryId, "Михайло Коцюбинський", "", "", 249, "in_stock", "Intermezzo"),
    ];

    public static readonly CatalogSearchFilterValues ContainerFilterValues = new(
        CategoryIds: [FictionRootCategoryId],
        CategoryRootId: FictionRootCategoryId,
        MinPrice: 200,
        MaxPrice: 350,
        Authors: ["Tolkien"],
        Genres: ["fantasy"],
        Format: ProductCatalogFormats.Paper,
        Name: "Hobbit",
        AvailabilityStatus: "in_stock");

    public static readonly CatalogSearchFilterValues SeedFilterValues = new(
        CategoryIds: [FictionRootCategoryId],
        CategoryRootId: FictionRootCategoryId,
        MinPrice: 330,
        MaxPrice: 360,
        Authors: ["Тарас Шевченко"],
        Genres: null,
        Format: null,
        Name: "Кобзар",
        AvailabilityStatus: "in_stock");

    public static IReadOnlyList<string> Match(
        IEnumerable<CatalogSearchFixtureProduct> products,
        CatalogSearchFilterValues filters,
        byte mask)
    {
        return products
            .Where(product => Matches(product, filters, mask))
            .Select(product => product.Slug)
            .OrderBy(slug => slug, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool Matches(
        CatalogSearchFixtureProduct product,
        CatalogSearchFilterValues filters,
        byte mask)
    {
        if (HasDimension(mask, CatalogSearchFilterDimension.Category)
            && product.RootCategoryId != filters.CategoryRootId)
            return false;

        if (HasDimension(mask, CatalogSearchFilterDimension.Price))
        {
            if (filters.MinPrice.HasValue && product.Price < filters.MinPrice.Value)
                return false;
            if (filters.MaxPrice.HasValue && product.Price > filters.MaxPrice.Value)
                return false;
        }

        if (HasDimension(mask, CatalogSearchFilterDimension.Author))
        {
            var authors = ProductCatalogFacetReader.NormalizeAuthors(filters.Authors);
            if (authors.Count == 0
                || !authors.Any(author =>
                    string.Equals(author, ProductCatalogFacetReader.Normalize(product.Author), StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (HasDimension(mask, CatalogSearchFilterDimension.Genre))
        {
            var genres = ProductCatalogFacetReader.NormalizeGenres(filters.Genres);
            if (genres.Count == 0
                || !genres.Any(genre =>
                    string.Equals(genre, ProductCatalogFacetReader.Normalize(product.Genre), StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (HasDimension(mask, CatalogSearchFilterDimension.Format))
        {
            var canonicalFormat = ProductCatalogFormats.Canonicalize(filters.Format);
            if (string.IsNullOrWhiteSpace(canonicalFormat)
                || !string.Equals(product.Format, canonicalFormat, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (HasDimension(mask, CatalogSearchFilterDimension.Name))
        {
            if (string.IsNullOrWhiteSpace(filters.Name)
                || !product.Name.Contains(filters.Name, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (HasDimension(mask, CatalogSearchFilterDimension.Availability)
            && !string.Equals(product.AvailabilityStatus, filters.AvailabilityStatus, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static bool HasDimension(byte mask, CatalogSearchFilterDimension dimension)
        => mask != 0 && (mask & (byte)dimension) == (byte)dimension;

    public static SearchCatalogProductsQuery BuildQuery(CatalogSearchFilterValues filters, byte mask)
    {
        var name = HasDimension(mask, CatalogSearchFilterDimension.Name) ? filters.Name : null;
        return new SearchCatalogProductsQuery(
            name,
            null,
            HasDimension(mask, CatalogSearchFilterDimension.Category) ? filters.CategoryIds : null,
            null,
            HasDimension(mask, CatalogSearchFilterDimension.Price) ? filters.MinPrice : null,
            HasDimension(mask, CatalogSearchFilterDimension.Price) ? filters.MaxPrice : null,
            HasDimension(mask, CatalogSearchFilterDimension.Availability) ? filters.AvailabilityStatus : null,
            HasDimension(mask, CatalogSearchFilterDimension.Author) ? filters.Authors : null,
            HasDimension(mask, CatalogSearchFilterDimension.Format) ? filters.Format : null,
            HasDimension(mask, CatalogSearchFilterDimension.Genre) ? filters.Genres : null,
            null,
            null,
            1,
            200,
            null);
    }
}

public static class CatalogSearchFilterCombinationGenerator
{
    public static IEnumerable<object[]> AllContainerCombinations()
    {
        for (byte mask = 1; mask < 128; mask++)
        {
            var expected = CatalogSearchFilterOracle.Match(
                CatalogSearchFilterOracle.ContainerProducts,
                CatalogSearchFilterOracle.ContainerFilterValues,
                mask);
            var query = CatalogSearchFilterOracle.BuildQuery(
                CatalogSearchFilterOracle.ContainerFilterValues,
                mask);

            yield return [mask, query, expected];
        }
    }

    public static IEnumerable<object[]> SeedE2ECombinations()
    {
        for (byte mask = 1; mask < 128; mask++)
        {
            if (CatalogSearchFilterOracle.HasDimension(mask, CatalogSearchFilterDimension.Genre)
                || CatalogSearchFilterOracle.HasDimension(mask, CatalogSearchFilterDimension.Format))
                continue;

            var expected = CatalogSearchFilterOracle.Match(
                CatalogSearchFilterOracle.SeedAnchorProducts,
                CatalogSearchFilterOracle.SeedFilterValues,
                mask);
            var query = CatalogSearchFilterOracle.BuildQuery(
                CatalogSearchFilterOracle.SeedFilterValues,
                mask);

            yield return [mask, query, expected];
        }
    }
}
