namespace Marketplace.Application.Behavior.DTOs;

public sealed record BehaviorSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    long TotalEvents,
    long ProductViews,
    long SearchQueries,
    long CatalogClicks,
    long AddToCartActions);

public sealed record TopQueryDto(string Query, long Count);

public sealed record ConversionFunnelDto(
    long ProductViews,
    long CatalogClicks,
    long AddToCartActions,
    decimal ClickThroughRate,
    decimal AddToCartRate);
