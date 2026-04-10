using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid CompanyId,
    long ProductId,
    Guid ActorUserId,
    bool IsActorAdmin,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? OldPrice,
    int MinStock,
    long CategoryId,
    bool HasVariants,
    ProductDetailDto? Detail,
    IReadOnlyList<ProductImageDto>? Images) : IRequest<Result<ProductDto>>;
