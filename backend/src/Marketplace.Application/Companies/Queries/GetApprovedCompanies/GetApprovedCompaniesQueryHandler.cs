using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetApprovedCompanies;

public sealed class GetApprovedCompaniesQueryHandler : IRequestHandler<GetApprovedCompaniesQuery, Result<IReadOnlyList<CompanyDto>>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;

    public GetApprovedCompaniesQueryHandler(ICompanyRepository companyRepository, IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> Handle(GetApprovedCompaniesQuery request, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<List<CompanyDto>>(CatalogCacheKeys.ApprovedCompanies, ct);
            if (cached is not null)
                return Result<IReadOnlyList<CompanyDto>>.Success(cached);

            var companies = await _companyRepository.GetApprovedAsync(ct);
            var dtos = companies.Select(CompanyMapper.ToDto).ToList();
            await _cache.SetAsync(CatalogCacheKeys.ApprovedCompanies, dtos, TimeSpan.FromMinutes(5), ct);
            return Result<IReadOnlyList<CompanyDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CompanyDto>>.Failure($"Failed to get approved companies: {ex.Message}");
        }
    }
}
