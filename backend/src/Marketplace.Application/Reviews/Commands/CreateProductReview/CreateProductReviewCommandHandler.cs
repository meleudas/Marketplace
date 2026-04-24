using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.CreateProductReview;

public sealed class CreateProductReviewCommandHandler : IRequestHandler<CreateProductReviewCommand, Result<ReviewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IReviewPurchaseVerificationService _verificationService;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public CreateProductReviewCommandHandler(
        IProductRepository productRepository,
        IProductReviewRepository productReviewRepository,
        IReviewPurchaseVerificationService verificationService,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _productRepository = productRepository;
        _productReviewRepository = productReviewRepository;
        _verificationService = verificationService;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result<ReviewDto>> Handle(CreateProductReviewCommand request, CancellationToken ct)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product = await _productRepository.GetByIdAsync(productId, ct);
            if (product is null)
                return Result.Failure<ReviewDto>("Product not found");

            var existing = await _productReviewRepository.GetByProductAndUserAsync(productId, request.ActorUserId, ct);
            if (existing is not null)
                return Result.Failure<ReviewDto>("Review already exists");

            var orderId = await _verificationService.GetVerifiedProductOrderIdAsync(request.ActorUserId, productId, ct);
            if (!orderId.HasValue)
                return Result.Failure<ReviewDto>("Verified purchase required");

            var review = ProductReview.Create(
                ProductReviewId.From(0),
                productId,
                request.ActorUserId,
                request.UserName,
                request.UserAvatar,
                request.Rating,
                request.Title,
                request.Comment,
                true,
                OrderId.From(orderId.Value));

            var saved = await _productReviewRepository.AddAsync(review, ct);
            await _ratingAggregationService.RecalculateProductAsync(productId, ct);
            await InvalidateProductCachesAsync(product, ct);
            return Result.Success(ReviewMapper.ToDto(saved, null));
        }
        catch (Exception ex)
        {
            return Result.Failure<ReviewDto>($"Failed to create review: {ex.Message}");
        }
    }

    private async Task InvalidateProductCachesAsync(Marketplace.Domain.Catalog.Entities.Product product, CancellationToken ct)
    {
        await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);
        await _cache.RemoveAsync(ReviewCacheKeys.ProductList(product.Id.Value, 1, 20), ct);
    }
}
