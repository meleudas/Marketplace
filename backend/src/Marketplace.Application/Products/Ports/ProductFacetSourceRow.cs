namespace Marketplace.Application.Products.Ports;

public sealed record ProductFacetSourceRow(
    long ProductId,
    string? AttributesRaw,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Brands);
