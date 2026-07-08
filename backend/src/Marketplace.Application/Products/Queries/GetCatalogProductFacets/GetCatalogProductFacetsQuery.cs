using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogProductFacets;

public sealed record GetCatalogProductFacetsQuery(
    IReadOnlyList<long>? CategoryIds = null,
    Guid? CompanyId = null) : IRequest<Result<CatalogProductFacetsDto>>;
