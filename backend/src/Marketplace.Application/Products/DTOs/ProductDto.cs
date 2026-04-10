namespace Marketplace.Application.Products.DTOs;

public sealed record ProductDto(
    ProductListItemDto Product,
    ProductDetailDto? Detail,
    IReadOnlyList<ProductImageDto> Images);
