using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Mappings;
using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.CreateCompanyReview;

public sealed class CreateCompanyReviewCommandHandler : IRequestHandler<CreateCompanyReviewCommand, Result<ReviewDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IReviewPurchaseVerificationService _verificationService;
    private readonly IReviewRatingAggregationService _ratingAggregationService;
    private readonly IAppCachePort _cache;

    public CreateCompanyReviewCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyReviewRepository companyReviewRepository,
        IReviewPurchaseVerificationService verificationService,
        IReviewRatingAggregationService ratingAggregationService,
        IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _companyReviewRepository = companyReviewRepository;
        _verificationService = verificationService;
        _ratingAggregationService = ratingAggregationService;
        _cache = cache;
    }

    public async Task<Result<ReviewDto>> Handle(CreateCompanyReviewCommand request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            var company = await _companyRepository.GetByIdAsync(companyId, ct);
            if (company is null)
                return Result.Failure<ReviewDto>("Company not found");

            var existing = await _companyReviewRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
            if (existing is not null)
                return Result.Failure<ReviewDto>("Review already exists");

            var orderId = await _verificationService.GetVerifiedCompanyOrderIdAsync(request.ActorUserId, companyId, ct);
            if (!orderId.HasValue)
                return Result.Failure<ReviewDto>("Verified purchase required");

            var review = Marketplace.Domain.Companies.Entities.CompanyReview.Create(
                CompanyReviewId.From(0),
                companyId,
                request.ActorUserId,
                request.UserName,
                orderId.Value,
                request.OverallRating,
                request.Comment);
            CompanyReview saved;
            try
            {
                saved = await _companyReviewRepository.AddAsync(review, ct);
            }
            catch
            {
                // Concurrent create can win the race between exists-check and insert.
                var concurrent = await _companyReviewRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
                if (concurrent is not null)
                    return Result.Failure<ReviewDto>("Review already exists");
                throw;
            }
            await _ratingAggregationService.RecalculateCompanyAsync(companyId, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + companyId.Value, ct);
            await _cache.RemoveAsync(ReviewCacheKeys.CompanyList(companyId.Value, 1, 20), ct);
            return Result.Success(ReviewMapper.ToDto(saved, null));
        }
        catch (Exception ex)
        {
            return Result.Failure<ReviewDto>($"Failed to create review: {ex.Message}");
        }
    }
}
