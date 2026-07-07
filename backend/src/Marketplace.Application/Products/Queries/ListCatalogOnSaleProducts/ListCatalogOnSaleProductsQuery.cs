using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.ListCatalogOnSaleProducts;

public sealed record ListCatalogOnSaleProductsQuery(
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinDiscountPercent,
    string? AvailabilityStatus,
    string? Sort,
    int Page,
    int PageSize,
    string? SearchAfter = null) : IRequest<Result<ProductSearchResultDto>>;
