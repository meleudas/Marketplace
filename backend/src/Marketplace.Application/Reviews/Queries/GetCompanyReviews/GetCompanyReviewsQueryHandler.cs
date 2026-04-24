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

namespace Marketplace.Application.Reviews.Queries.GetCompanyReviews;

public sealed class GetCompanyReviewsQueryHandler : IRequestHandler<GetCompanyReviewsQuery, Result<ReviewListDto>>
{
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewReplyRepository _reviewReplyRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetCompanyReviewsQueryHandler(
        ICompanyReviewRepository companyReviewRepository,
        IReviewReplyRepository reviewReplyRepository,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _companyReviewRepository = companyReviewRepository;
        _reviewReplyRepository = reviewReplyRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<ReviewListDto>> Handle(GetCompanyReviewsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.Size, 1, 100);
        var key = ReviewCacheKeys.CompanyList(request.CompanyId, page, size);
        var cached = await _cache.GetAsync<ReviewListDto>(key, ct);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _companyReviewRepository.ListByCompanyAsync(CompanyId.From(request.CompanyId), page, size, ct);
        var mapped = new List<ReviewDto>(items.Count);
        foreach (var item in items)
        {
            var reply = await _reviewReplyRepository.GetByCompanyReviewIdAsync(item.Id, ct);
            mapped.Add(ReviewMapper.ToDto(item, reply));
        }

        var dto = new ReviewListDto(page, size, mapped);
        await _cache.SetAsync(key, dto, _ttl.CatalogCompanyReviews, ct);
        return Result.Success(dto);
    }
}
