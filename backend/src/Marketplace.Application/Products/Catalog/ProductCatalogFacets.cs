namespace Marketplace.Application.Products.Catalog;

public sealed record ProductCatalogFacets(
    string? Author,
    string? Format,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags);
