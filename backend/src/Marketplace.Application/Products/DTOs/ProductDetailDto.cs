namespace Marketplace.Application.Products.DTOs;

public sealed record ProductDetailDto(
    string Slug,
    string? AttributesRaw,
    string? VariantsRaw,
    string? SpecificationsRaw,
    string? SeoRaw,
    string? ContentBlocksRaw,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Brands);
