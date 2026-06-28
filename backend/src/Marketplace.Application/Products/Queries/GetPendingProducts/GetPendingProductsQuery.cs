using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetPendingProducts;

public sealed record GetPendingProductsQuery : IRequest<Result<IReadOnlyList<PendingProductModerationDto>>>;
