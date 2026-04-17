using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Companies.Queries.GetAdminCompanyById;

public sealed class GetAdminCompanyByIdQueryHandler : IRequestHandler<GetAdminCompanyByIdQuery, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetAdminCompanyByIdQueryHandler(ICompanyRepository companyRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _companyRepository = companyRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<CompanyDto>> Handle(GetAdminCompanyByIdQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = CatalogCacheKeys.AdminCompanyByIdPrefix + request.CompanyId;
            var cached = await _cache.GetAsync<CompanyDto>(cacheKey, ct);
            if (cached is not null)
                return Result<CompanyDto>.Success(cached);

            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company is null)
                return Result<CompanyDto>.Failure("Company not found");

            var dto = CompanyMapper.ToDto(company);
            await _cache.SetAsync(cacheKey, dto, _ttl.AdminCompany, ct);
            return Result<CompanyDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to get company: {ex.Message}");
        }
    }
}
