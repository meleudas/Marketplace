using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyLegalProfileRepository _companyLegalProfileRepository;
    private readonly IAppCachePort _cache;

    public CreateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyLegalProfileRepository companyLegalProfileRepository,
        IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _companyLegalProfileRepository = companyLegalProfileRepository;
        _cache = cache;
    }

    public async Task<Result<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var id = CompanyId.From(Guid.NewGuid());

            var company = Company.Create(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.ImageUrl,
                request.ContactEmail,
                request.ContactPhone,
                CompanyMapper.ToAddress(request.Address),
                new JsonBlob(request.MetaRaw));

            await _companyRepository.AddAsync(company, ct);
            var legalType = request.LegalProfile.LegalType.Trim().ToLowerInvariant() switch
            {
                "individual" => CompanyLegalType.Individual,
                "entrepreneur" => CompanyLegalType.Entrepreneur,
                "llc" => CompanyLegalType.Llc,
                "jsc" => CompanyLegalType.Jsc,
                _ => throw new InvalidOperationException("Unsupported legal type")
            };
            var legalProfile = CompanyLegalProfile.Create(
                CompanyLegalProfileId.From(0),
                company.Id,
                request.LegalProfile.LegalName,
                legalType,
                request.LegalProfile.Edrpou,
                request.LegalProfile.Ipn,
                request.LegalProfile.CertificateNumber,
                request.LegalProfile.IsVatPayer,
                request.LegalProfile.InitialCommissionPercent);
            await _companyLegalProfileRepository.AddAsync(legalProfile, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + company.Slug.ToLowerInvariant(), ct);
            return Result<CompanyDto>.Success(CompanyMapper.ToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to create company: {ex.Message}");
        }
    }
}
