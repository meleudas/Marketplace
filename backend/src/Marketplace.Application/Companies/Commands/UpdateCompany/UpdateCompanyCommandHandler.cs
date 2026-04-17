using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;

    public UpdateCompanyCommandHandler(ICompanyRepository companyRepository, IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _cache = cache;
    }

    public async Task<Result<CompanyDto>> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var id = CompanyId.From(request.CompanyId);
            var company = await _companyRepository.GetByIdAsync(id, ct);
            if (company == null)
                return Result<CompanyDto>.Failure("Company not found");
            var oldSlug = company.Slug;

            company.UpdateProfile(
                request.Name,
                request.Slug,
                request.Description,
                request.ImageUrl,
                request.ContactEmail,
                request.ContactPhone,
                CompanyMapper.ToAddress(request.Address),
                new JsonBlob(request.MetaRaw));

            await _companyRepository.UpdateAsync(company, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + oldSlug.ToLowerInvariant(), ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + company.Slug.ToLowerInvariant(), ct);
            return Result<CompanyDto>.Success(CompanyMapper.ToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to update company: {ex.Message}");
        }
    }
}
