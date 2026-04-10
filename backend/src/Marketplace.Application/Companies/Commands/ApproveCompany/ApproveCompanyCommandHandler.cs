using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.ApproveCompany;

public sealed class ApproveCompanyCommandHandler : IRequestHandler<ApproveCompanyCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;

    public ApproveCompanyCommandHandler(ICompanyRepository companyRepository, IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(ApproveCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company == null)
                return Result.Failure("Company not found");

            company.Approve(request.AdminUserId.ToString());
            await _companyRepository.UpdateAsync(company, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to approve company: {ex.Message}");
        }
    }
}
