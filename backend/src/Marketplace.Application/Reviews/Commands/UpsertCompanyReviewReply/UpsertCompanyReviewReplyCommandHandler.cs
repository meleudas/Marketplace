using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Authorization;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpsertCompanyReviewReply;

public sealed class UpsertCompanyReviewReplyCommandHandler : IRequestHandler<UpsertCompanyReviewReplyCommand, Result<ReviewReplyDto>>
{
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewAccessService _reviewAccessService;
    private readonly IAppCachePort _cache;

    public UpsertCompanyReviewReplyCommandHandler(
        ICompanyReviewRepository companyReviewRepository,
        IReviewReplyRepository reviewReplyRepository,
        IReviewAccessService reviewAccessService,
        IAppCachePort cache)
    {
        _companyReviewRepository = companyReviewRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _reviewAccessService = reviewAccessService;
        _cache = cache;
    }

    public async Task<Result<ReviewReplyDto>> Handle(UpsertCompanyReviewReplyCommand request, CancellationToken ct)
    {
        var review = await _companyReviewRepository.GetByIdAsync(CompanyReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure<ReviewReplyDto>("Review not found");

        var hasAccess = await _reviewAccessService.HasCompanyAccessAsync(
            review.CompanyId,
            request.ActorUserId,
            request.IsActorAdmin,
            ReviewPermission.ReplyAsCompany,
            ct);
        if (!hasAccess)
            return Result.Failure<ReviewReplyDto>("Forbidden");

        var existing = await _reviewReplyRepository.GetByCompanyReviewIdAsync(review.Id, ct);
        if (existing is null)
        {
            var created = ReviewReply.CreateForCompanyReview(
                ReviewReplyId.From(0),
                review.Id,
                review.CompanyId,
                request.ActorUserId,
                request.Body);
            var saved = await _reviewReplyRepository.AddAsync(created, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + review.CompanyId.Value, ct);
            return Result.Success(ReviewMapper.ToReplyDto(saved));
        }

        existing.UpdateBody(request.Body);
        await _reviewReplyRepository.UpdateAsync(existing, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + review.CompanyId.Value, ct);
        return Result.Success(ReviewMapper.ToReplyDto(existing));
    }
}
