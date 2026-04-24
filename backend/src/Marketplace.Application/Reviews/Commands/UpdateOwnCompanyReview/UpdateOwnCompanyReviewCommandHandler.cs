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

namespace Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;

public sealed class UpdateOwnCompanyReviewCommandHandler : IRequestHandler<UpdateOwnCompanyReviewCommand, Result<ReviewDto>>
{
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public UpdateOwnCompanyReviewCommandHandler(
        ICompanyReviewRepository companyReviewRepository,
        IReviewReplyRepository reviewReplyRepository,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _companyReviewRepository = companyReviewRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result<ReviewDto>> Handle(UpdateOwnCompanyReviewCommand request, CancellationToken ct)
    {
        try
        {
            var review = await _companyReviewRepository.GetByIdAsync(CompanyReviewId.From(request.ReviewId), ct);
            if (review is null)
                return Result.Failure<ReviewDto>("Review not found");
            if (review.UserId != request.ActorUserId)
                return Result.Failure<ReviewDto>("Forbidden");

            review.Update(request.OverallRating, request.Comment);
            await _companyReviewRepository.UpdateAsync(review, ct);
            await _ratingAggregationService.RecalculateCompanyAsync(review.CompanyId, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + review.CompanyId.Value, ct);
            await _cache.RemoveAsync(ReviewCacheKeys.CompanyList(review.CompanyId.Value, 1, 20), ct);
            var reply = await _reviewReplyRepository.GetByCompanyReviewIdAsync(review.Id, ct);
            return Result.Success(ReviewMapper.ToDto(review, reply));
        }
        catch (Exception ex)
        {
            return Result.Failure<ReviewDto>($"Failed to update review: {ex.Message}");
        }
    }
}
