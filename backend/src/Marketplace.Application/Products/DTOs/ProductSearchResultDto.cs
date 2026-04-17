namespace Marketplace.Application.Products.DTOs;

public sealed record ProductSearchResultDto(
    IReadOnlyList<ProductListItemDto> Items,
    long Total,
    int Page,
    int PageSize);
