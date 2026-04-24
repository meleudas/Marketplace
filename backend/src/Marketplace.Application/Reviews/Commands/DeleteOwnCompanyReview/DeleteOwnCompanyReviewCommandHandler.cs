using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;

public sealed class DeleteOwnCompanyReviewCommandHandler : IRequestHandler<DeleteOwnCompanyReviewCommand, Result>
{
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public DeleteOwnCompanyReviewCommandHandler(
        ICompanyReviewRepository companyReviewRepository,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _companyReviewRepository = companyReviewRepository;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteOwnCompanyReviewCommand request, CancellationToken ct)
    {
        var review = await _companyReviewRepository.GetByIdAsync(CompanyReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure("Review not found");
        if (review.UserId != request.ActorUserId)
            return Result.Failure("Forbidden");

        var now = DateTime.UtcNow;
        await _companyReviewRepository.SoftDeleteAsync(review.Id, now, ct);
        await _ratingAggregationService.RecalculateCompanyAsync(review.CompanyId, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + review.CompanyId.Value, ct);
        await _cache.RemoveAsync(ReviewCacheKeys.CompanyList(review.CompanyId.Value, 1, 20), ct);
        return Result.Success();
    }
}
