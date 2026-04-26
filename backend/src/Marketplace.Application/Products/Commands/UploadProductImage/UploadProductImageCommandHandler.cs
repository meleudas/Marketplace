using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.UploadProductImage;

public sealed class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, Result<ProductImageDto>>
{
    private readonly IProductAccessService _access;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IObjectStorage _storage;
    private readonly IProductImageProcessingDispatcher _processingDispatcher;

    public UploadProductImageCommandHandler(
        IProductAccessService access,
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IObjectStorage storage,
        IProductImageProcessingDispatcher processingDispatcher)
    {
        _access = access;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _storage = storage;
        _processingDispatcher = processingDispatcher;
    }

    public async Task<Result<ProductImageDto>> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, ProductPermission.WriteProduct, ct))
            return Result<ProductImageDto>.Failure("Forbidden");

        var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
        if (product is null || product.CompanyId.Value != request.CompanyId)
            return Result<ProductImageDto>.Failure("Product not found");

        var ext = Path.GetExtension(request.FileName);
        var objectKey = $"products/{request.ProductId}/original/{Guid.NewGuid():N}{ext}";

        await using var stream = new MemoryStream(request.Content);
        await _storage.UploadAsync(objectKey, stream, request.ContentType, ct);
        var originalUrl = _storage.GetPublicUrl(objectKey);

        var images = (await _productImageRepository.ListByProductIdAsync(product.Id, ct)).ToList();
        var image = ProductImage.Create(
            ProductImageId.From(0),
            product.Id,
            originalUrl,
            string.Empty,
            objectKey,
            objectKey,
            string.Empty,
            request.AltText,
            request.SortOrder,
            request.IsMain,
            null,
            null,
            request.Content.LongLength);
        images.Add(image);
        await _productImageRepository.ReplaceForProductAsync(product.Id, images, ct);

        await _processingDispatcher.EnqueueDerivativesAsync(product.Id.Value, objectKey, ct);

        return Result<ProductImageDto>.Success(new ProductImageDto(
            image.ImageUrl,
            image.ThumbnailUrl,
            image.AltText,
            image.SortOrder,
            image.IsMain,
            image.Width,
            image.Height,
            image.FileSize,
            "processing"));
    }
}
