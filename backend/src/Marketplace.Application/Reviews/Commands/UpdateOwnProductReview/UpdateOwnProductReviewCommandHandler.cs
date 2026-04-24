using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;

public sealed class UpdateOwnProductReviewCommandHandler : IRequestHandler<UpdateOwnProductReviewCommand, Result<ReviewDto>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public UpdateOwnProductReviewCommandHandler(
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

    public async Task<Result<ReviewDto>> Handle(UpdateOwnProductReviewCommand request, CancellationToken ct)
    {
        try
        {
            var review = await _productReviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
            if (review is null)
                return Result.Failure<ReviewDto>("Review not found");
            if (review.UserId != request.ActorUserId)
                return Result.Failure<ReviewDto>("Forbidden");

            review.Update(request.Title, request.Comment, request.Rating);
            await _productReviewRepository.UpdateAsync(review, ct);
            await _ratingAggregationService.RecalculateProductAsync(review.ProductId, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            await _cache.RemoveAsync(ReviewCacheKeys.ProductList(review.ProductId.Value, 1, 20), ct);
            var reply = await _reviewReplyRepository.GetByProductReviewIdAsync(review.Id, ct);
            return Result.Success(ReviewMapper.ToDto(review, reply));
        }
        catch (Exception ex)
        {
            return Result.Failure<ReviewDto>($"Failed to update review: {ex.Message}");
        }
    }
}
