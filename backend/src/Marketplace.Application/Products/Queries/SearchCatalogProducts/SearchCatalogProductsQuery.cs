using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.SearchCatalogProducts;

public sealed record SearchCatalogProductsQuery(
    string? Name,
    string? Query,
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    string? Sort,
    int Page = 1,
    int PageSize = 20,
    string? SearchAfter = null) : IRequest<Result<ProductSearchResultDto>>;
