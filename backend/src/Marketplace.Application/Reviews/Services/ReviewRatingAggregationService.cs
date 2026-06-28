using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Reviews.Repositories;

namespace Marketplace.Application.Reviews.Services;

public sealed class ReviewRatingAggregationService : IReviewRatingAggregationService
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly ICompanyReviewRepository _companyReviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICompanyRepository _companyRepository;

    public ReviewRatingAggregationService(
        IProductReviewRepository productReviewRepository,
        ICompanyReviewRepository companyReviewRepository,
        IProductRepository productRepository,
        ICompanyRepository companyRepository)
    {
        _productReviewRepository = productReviewRepository;
        _companyReviewRepository = companyReviewRepository;
        _productRepository = productRepository;
        _companyRepository = companyRepository;
    }

    public async Task RecalculateProductAsync(ProductId productId, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return;

        var stats = await _productReviewRepository.GetApprovedStatsAsync(productId, ct);
        product.SetReviewStats(stats.Average, stats.Count);
        await _productRepository.UpdateAsync(product, ct);
    }

    public async Task RecalculateCompanyAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, ct);
        if (company is null)
            return;

        var stats = await _companyReviewRepository.GetApprovedStatsAsync(companyId, ct);
        company.SetReviewStats(stats.Average, stats.Count);
        await _companyRepository.UpdateAsync(company, ct);
    }
}
