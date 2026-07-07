namespace Marketplace.Application.Products.Catalog;

public sealed record ProductCatalogFacets(
    string? Author,
    IReadOnlyList<string> AuthorValues,
    string? Format,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags);
