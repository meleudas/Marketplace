using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.ModerateCompanyReview;

public sealed class ModerateCompanyReviewCommandHandler : IRequestHandler<ModerateCompanyReviewCommand, Result<ReviewDto>>
{
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public ModerateCompanyReviewCommandHandler(
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

    public async Task<Result<ReviewDto>> Handle(ModerateCompanyReviewCommand request, CancellationToken ct)
    {
        if (!request.CanModerate)
            return Result.Failure<ReviewDto>("Forbidden");

        var review = await _companyReviewRepository.GetByIdAsync(CompanyReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure<ReviewDto>("Review not found");

        review.Moderate(request.Status, request.ActorUserId);
        await _companyReviewRepository.UpdateAsync(review, ct);
        await _ratingAggregationService.RecalculateCompanyAsync(review.CompanyId, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + review.CompanyId.Value, ct);
        var reply = await _reviewReplyRepository.GetByCompanyReviewIdAsync(review.Id, ct);
        return Result.Success(ReviewMapper.ToDto(review, reply));
    }
}
