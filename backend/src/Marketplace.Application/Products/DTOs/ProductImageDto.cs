namespace Marketplace.Application.Products.DTOs;

public sealed record ProductImageDto(
    string ImageUrl,
    string ThumbnailUrl,
    string AltText,
    int SortOrder,
    bool IsMain,
    int? Width,
    int? Height,
    long? FileSize);
