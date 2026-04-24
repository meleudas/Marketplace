using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;

public sealed class DeleteOwnProductReviewCommandHandler : IRequestHandler<DeleteOwnProductReviewCommand, Result>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public DeleteOwnProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _productReviewRepository = productReviewRepository;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteOwnProductReviewCommand request, CancellationToken ct)
    {
        var review = await _productReviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure("Review not found");
        if (review.UserId != request.ActorUserId)
            return Result.Failure("Forbidden");

        var now = DateTime.UtcNow;
        await _productReviewRepository.SoftDeleteAsync(review.Id, now, ct);
        await _ratingAggregationService.RecalculateProductAsync(review.ProductId, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
        await _cache.RemoveAsync(ReviewCacheKeys.ProductList(review.ProductId.Value, 1, 20), ct);
        return Result.Success();
    }
}
