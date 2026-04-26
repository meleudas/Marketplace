using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.UploadProductImage;

public sealed record UploadProductImageCommand(
    Guid CompanyId,
    long ProductId,
    Guid ActorUserId,
    bool IsActorAdmin,
    byte[] Content,
    string FileName,
    string ContentType,
    string AltText,
    int SortOrder,
    bool IsMain) : IRequest<Result<ProductImageDto>>;
