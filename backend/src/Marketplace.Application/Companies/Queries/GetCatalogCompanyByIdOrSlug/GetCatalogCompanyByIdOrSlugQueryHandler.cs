using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;

public sealed class GetCatalogCompanyByIdOrSlugQueryHandler : IRequestHandler<GetCatalogCompanyByIdOrSlugQuery, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetCatalogCompanyByIdOrSlugQueryHandler(ICompanyRepository companyRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _companyRepository = companyRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<CompanyDto>> Handle(GetCatalogCompanyByIdOrSlugQuery request, CancellationToken ct)
    {
        try
        {
            var key = request.IdOrSlug.Trim();
            if (string.IsNullOrEmpty(key))
                return Result<CompanyDto>.Failure("Company not found");

            if (Guid.TryParse(key, out var guidKey))
            {
                var cachedById = await _cache.GetAsync<CompanyDto>(CatalogCacheKeys.CatalogCompanyByIdPrefix + guidKey, ct);
                if (cachedById is not null)
                    return Result<CompanyDto>.Success(cachedById);
            }
            else
            {
                var cachedBySlug = await _cache.GetAsync<CompanyDto>(CatalogCacheKeys.CatalogCompanyBySlugPrefix + key.ToLowerInvariant(), ct);
                if (cachedBySlug is not null)
                    return Result<CompanyDto>.Success(cachedBySlug);
            }

            Company? company = null;
            if (Guid.TryParse(key, out var guid))
            {
                company = await _companyRepository.GetByIdAsync(CompanyId.From(guid), ct);
                if (company is not null && (!company.IsApproved || company.IsDeleted))
                    company = null;
            }
            else
            {
                company = await _companyRepository.GetApprovedNotDeletedBySlugAsync(key, ct);
            }

            if (company is null)
                return Result<CompanyDto>.Failure("Company not found");

            var dto = CompanyMapper.ToDto(company);
            await _cache.SetAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + company.Id.Value, dto, _ttl.CatalogCompany, ct);
            await _cache.SetAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + company.Slug.ToLowerInvariant(), dto, _ttl.CatalogCompany, ct);
            return Result<CompanyDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to get company: {ex.Message}");
        }
    }
}
