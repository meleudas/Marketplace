using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogAuthors;

public sealed record GetCatalogAuthorsQuery(
    IReadOnlyList<long>? CategoryIds = null,
    Guid? CompanyId = null) : IRequest<Result<IReadOnlyList<CatalogFacetOptionDto>>>;
