using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Reviews.Queries.GetProductReviews;

public sealed class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, Result<ReviewListDto>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetProductReviewsQueryHandler(
        IProductReviewRepository productReviewRepository,
        IReviewReplyRepository reviewReplyRepository,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _productReviewRepository = productReviewRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<ReviewListDto>> Handle(GetProductReviewsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.Size, 1, 100);
        var key = ReviewCacheKeys.ProductList(request.ProductId, page, size);
        var cached = await _cache.GetAsync<ReviewListDto>(key, ct);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _productReviewRepository.ListByProductAsync(ProductId.From(request.ProductId), page, size, ct);
        var mapped = new List<ReviewDto>(items.Count);
        foreach (var item in items)
        {
            var reply = await _reviewReplyRepository.GetByProductReviewIdAsync(item.Id, ct);
            mapped.Add(ReviewMapper.ToDto(item, reply));
        }

        var dto = new ReviewListDto(page, size, mapped);
        await _cache.SetAsync(key, dto, _ttl.CatalogProductReviews, ct);
        return Result.Success(dto);
    }
}
