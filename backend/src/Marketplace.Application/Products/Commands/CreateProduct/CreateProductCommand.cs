using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    Guid CompanyId,
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
