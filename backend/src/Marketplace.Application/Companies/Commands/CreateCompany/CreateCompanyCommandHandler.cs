using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;

    public CreateCompanyCommandHandler(ICompanyRepository companyRepository, IAppCachePort cache)
    {
        _companyRepository = companyRepository;
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
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            return Result<CompanyDto>.Success(CompanyMapper.ToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to create company: {ex.Message}");
        }
    }
}
