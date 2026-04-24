using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Authorization;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpsertProductReviewReply;

public sealed class UpsertProductReviewReplyCommandHandler : IRequestHandler<UpsertProductReviewReplyCommand, Result<ReviewReplyDto>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IReviewAccessService _reviewAccessService;
    private readonly IAppCachePort _cache;

    public UpsertProductReviewReplyCommandHandler(
        IProductReviewRepository productReviewRepository,
        IProductRepository productRepository,
        IReviewReplyRepository reviewReplyRepository,
        IReviewAccessService reviewAccessService,
        IAppCachePort cache)
    {
        _productReviewRepository = productReviewRepository;
        _productRepository = productRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _reviewAccessService = reviewAccessService;
        _cache = cache;
    }

    public async Task<Result<ReviewReplyDto>> Handle(UpsertProductReviewReplyCommand request, CancellationToken ct)
    {
        var review = await _productReviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Result.Failure<ReviewReplyDto>("Review not found");

        var product = await _productRepository.GetByIdAsync(review.ProductId, ct);
        if (product is null)
            return Result.Failure<ReviewReplyDto>("Product not found");
        var hasAccess = await _reviewAccessService.HasCompanyAccessAsync(product.CompanyId, request.ActorUserId, request.IsActorAdmin, ReviewPermission.ReplyAsCompany, ct);
        if (!hasAccess)
            return Result.Failure<ReviewReplyDto>("Forbidden");

        var existing = await _reviewReplyRepository.GetByProductReviewIdAsync(review.Id, ct);
        if (existing is null)
        {
            var created = ReviewReply.CreateForProductReview(
                ReviewReplyId.From(0),
                review.Id,
                product.CompanyId,
                request.ActorUserId,
                request.Body);
            var saved = await _reviewReplyRepository.AddAsync(created, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            return Result.Success(ReviewMapper.ToReplyDto(saved));
        }

        existing.UpdateBody(request.Body);
        await _reviewReplyRepository.UpdateAsync(existing, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
        return Result.Success(ReviewMapper.ToReplyDto(existing));
    }
}
