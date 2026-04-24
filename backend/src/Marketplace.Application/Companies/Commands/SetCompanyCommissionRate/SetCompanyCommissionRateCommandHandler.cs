using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;

public sealed class SetCompanyCommissionRateCommandHandler : IRequestHandler<SetCompanyCommissionRateCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyContractRepository _companyContractRepository;
    private readonly ICompanyCommissionRateRepository _companyCommissionRateRepository;
    private readonly IAppCachePort _cache;

    public SetCompanyCommissionRateCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyContractRepository companyContractRepository,
        ICompanyCommissionRateRepository companyCommissionRateRepository,
        IAppCachePort cache)
    {
        _companyRepository = companyRepository;
        _companyContractRepository = companyContractRepository;
        _companyCommissionRateRepository = companyCommissionRateRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(SetCompanyCommissionRateCommand request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            var company = await _companyRepository.GetByIdAsync(companyId, ct);
            if (company is null)
                return Result.Failure("Company not found");

            var activeContract = await _companyContractRepository.GetActiveByCompanyIdAsync(companyId, ct);
            if (activeContract is null)
                return Result.Failure("Active contract not found");

            var currentRate = await _companyCommissionRateRepository.GetActiveByCompanyIdAsync(companyId, ct);
            if (currentRate is not null && currentRate.EffectiveFrom >= request.EffectiveFrom)
                return Result.Failure("New commission effective_from must be greater than current effective_from");

            if (currentRate is not null)
            {
                currentRate.Close(request.EffectiveFrom);
                await _companyCommissionRateRepository.UpdateAsync(currentRate, ct);
            }

            var rate = CompanyCommissionRate.Create(
                CompanyCommissionRateId.From(0),
                companyId,
                activeContract.Id,
                request.CommissionPercent,
                request.EffectiveFrom,
                request.Reason,
                request.AdminUserId);
            await _companyCommissionRateRepository.AddAsync(rate, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyCommissionRatesPrefix + request.CompanyId, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCompanyByIdPrefix + request.CompanyId, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to set commission rate: {ex.Message}");
        }
    }
}
