using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.ApproveCompany;

public sealed class ApproveCompanyCommandHandler : IRequestHandler<ApproveCompanyCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyLegalProfileRepository _companyLegalProfileRepository;
    private readonly ICompanyContractRepository _companyContractRepository;
    private readonly ICompanyCommissionRateRepository _companyCommissionRateRepository;
    private readonly IAppCachePort _cache;

    public ApproveCompanyCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyLegalProfileRepository companyLegalProfileRepository,
        ICompanyContractRepository companyContractRepository,
        ICompanyCommissionRateRepository companyCommissionRateRepository,
        IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _companyLegalProfileRepository = companyLegalProfileRepository;
        _companyContractRepository = companyContractRepository;
        _companyCommissionRateRepository = companyCommissionRateRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(ApproveCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company == null)
                return Result.Failure("Company not found");
            var legalProfile = await _companyLegalProfileRepository.GetByCompanyIdAsync(company.Id, ct);
            if (legalProfile == null)
                return Result.Failure("Company legal profile not found");

            company.Approve(request.AdminUserId.ToString());
            await _companyRepository.UpdateAsync(company, ct);
            var activeContract = await _companyContractRepository.GetActiveByCompanyIdAsync(company.Id, ct);
            if (activeContract == null)
            {
                var contract = CompanyContract.CreateActive(
                    CompanyContractId.From(0),
                    company.Id,
                    $"CMP-{DateTime.UtcNow:yyyyMMdd}-{company.Id.Value.ToString()[..8].ToUpperInvariant()}",
                    DateTime.UtcNow,
                    "Auto-created on company approval");
                activeContract = await _companyContractRepository.AddAsync(contract, ct);
            }

            var activeRate = await _companyCommissionRateRepository.GetActiveByCompanyIdAsync(company.Id, ct);
            if (activeRate == null)
            {
                var initialRate = CompanyCommissionRate.Create(
                    CompanyCommissionRateId.From(0),
                    company.Id,
                    activeContract.Id,
                    legalProfile.InitialCommissionPercent ?? 10m,
                    DateTime.UtcNow,
                    "Initial rate on approval",
                    request.AdminUserId);
                await _companyCommissionRateRepository.AddAsync(initialRate, ct);
            }
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + company.Slug.ToLowerInvariant(), ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyContractsPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyCommissionRatesPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyLegalProfilePrefix + company.Id.Value, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to approve company: {ex.Message}");
        }
    }
}
