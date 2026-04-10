using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogProducts;

public sealed record GetCatalogProductsQuery : IRequest<Result<IReadOnlyList<ProductListItemDto>>>;
