using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductAccessService _access;
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _detailRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly IAppCachePort _cache;
    private readonly IAppNotificationScheduler _appNotifications;

    public CreateProductCommandHandler(
        IProductAccessService access,
        IProductRepository productRepository,
        IProductDetailRepository detailRepository,
        IProductImageRepository imageRepository,
        IAppCachePort cache,
        IAppNotificationScheduler appNotifications)
    {
        _access = access;
        _productRepository = productRepository;
        _detailRepository = detailRepository;
        _imageRepository = imageRepository;
        _cache = cache;
        _appNotifications = appNotifications;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, ProductPermission.WriteProduct, ct))
                return Result<ProductDto>.Failure("Forbidden");

            Product product;
            try
            {
                product = Product.Create(
                    ProductId.From(0),
                    CompanyId.From(request.CompanyId),
                    request.Name,
                    request.Slug,
                    request.Description,
                    new Money(request.Price),
                    request.OldPrice.HasValue ? new Money(request.OldPrice.Value) : null,
                    stock: 0,
                    minStock: request.MinStock,
                    categoryId: CategoryId.From(request.CategoryId),
                    hasVariants: request.HasVariants);
                product.SubmitForModeration(request.ActorUserId);
            }
            catch (DomainException ex)
            {
                return Result<ProductDto>.Failure(ex.Message);
            }

            await _productRepository.AddAsync(product, ct);
            product = await _productRepository.GetBySlugAsync(CompanyId.From(request.CompanyId), request.Slug, ct)
                ?? product;

            ProductDetail? detail = null;
            if (request.Detail is not null)
            {
                detail = ProductDetail.Create(
                    ProductDetailId.From(0),
                    product.Id,
                    request.Detail.Slug,
                    new JsonBlob(request.Detail.AttributesRaw),
                    new JsonBlob(request.Detail.VariantsRaw),
                    new JsonBlob(request.Detail.SpecificationsRaw),
                    new JsonBlob(request.Detail.SeoRaw),
                    new JsonBlob(request.Detail.ContentBlocksRaw),
                    request.Detail.Tags,
                    request.Detail.Brands);
                await _detailRepository.AddAsync(detail, ct);
            }

            var images = (request.Images ?? [])
                .Select(x => ProductImage.Create(
                    ProductImageId.From(0),
                    product.Id,
                    x.ImageUrl,
                    x.ThumbnailUrl,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    x.AltText,
                    x.SortOrder,
                    x.IsMain,
                    x.Width,
                    x.Height,
                    x.FileSize))
                .ToList();
            await _imageRepository.ReplaceForProductAsync(product.Id, images, ct);

            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            await _appNotifications.ScheduleAsync(
                new AppNotificationRequest
                {
                    TemplateKey = AppNotificationTemplateKeys.AdminProductPendingReview,
                    CorrelationId = AppNotificationCorrelationIds.ProductPendingReviewQueue(product.Id.Value),
                    Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                    Audience = AppNotificationAudienceKind.Admins,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        productId = product.Id.Value,
                        companyId = product.CompanyId.Value,
                        name = product.Name,
                        slug = product.Slug
                    })
                },
                ct);

            var dto = new ProductDto(
                ProductMapper.ToListItemDto(product, 0, "out_of_stock"),
                detail is null ? null : ProductMapper.ToDetailDto(detail),
                images.Select(ProductMapper.ToImageDto).ToList());
            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure($"Failed to create product: {ex.Message}");
        }
    }
}
