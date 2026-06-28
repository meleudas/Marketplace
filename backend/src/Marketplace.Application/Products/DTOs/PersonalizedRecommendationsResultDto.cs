namespace Marketplace.Application.Products.DTOs;

public sealed record PersonalizedRecommendationsResultDto(
    Guid UserId,
    string ModelVersion,
    bool UsedFallback,
    IReadOnlyList<ProductListItemDto> Items);
