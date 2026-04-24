using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.ModerateProductReview;

public sealed class ModerateProductReviewCommandHandler : IRequestHandler<ModerateProductReviewCommand, Result<ReviewDto>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public ModerateProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IReviewReplyRepository reviewReplyRepository,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _productReviewRepository = productReviewRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result<ReviewDto>> Handle(ModerateProductReviewCommand request, CancellationToken ct)
    {
        if (!request.CanModerate)
            return Result.Failure<ReviewDto>("Forbidden");

        var review = await _productReviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure<ReviewDto>("Review not found");

        review.Moderate(request.Status, request.ActorUserId);
        await _productReviewRepository.UpdateAsync(review, ct);
        await _ratingAggregationService.RecalculateProductAsync(review.ProductId, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
        var reply = await _reviewReplyRepository.GetByProductReviewIdAsync(review.Id, ct);
        return Result.Success(ReviewMapper.ToDto(review, reply));
    }
}
