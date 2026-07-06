using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.ListCatalogNewProducts;

public sealed record ListCatalogNewProductsQuery(
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    int Page,
    int PageSize,
    string? SearchAfter = null) : IRequest<Result<ProductSearchResultDto>>;
