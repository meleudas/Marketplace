using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCompanyProducts;

public sealed record GetCompanyProductsQuery(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<IReadOnlyList<ProductListItemDto>>>;
