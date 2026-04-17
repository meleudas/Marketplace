using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.RevokeCompanyApproval;

public sealed class RevokeCompanyApprovalCommandHandler : IRequestHandler<RevokeCompanyApprovalCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;

    public RevokeCompanyApprovalCommandHandler(ICompanyRepository companyRepository, IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(RevokeCompanyApprovalCommand request, CancellationToken ct)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company == null)
                return Result.Failure("Company not found");

            company.RevokeApproval();
            await _companyRepository.UpdateAsync(company, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ApprovedCompanies, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyByIdPrefix + company.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCompanyBySlugPrefix + company.Slug.ToLowerInvariant(), ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to revoke company approval: {ex.Message}");
        }
    }
}
