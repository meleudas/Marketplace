namespace Marketplace.Application.Products.DTOs;

public sealed record SimilarProductsResultDto(long SourceProductId, IReadOnlyList<ProductListItemDto> Items);
