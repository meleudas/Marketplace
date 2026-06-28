using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Queries.ListAdminCommissionRates;

public sealed record ListAdminCommissionRatesQuery(Guid CompanyId) : IRequest<Result<IReadOnlyList<CompanyCommissionRateHistoryDto>>>;

public sealed class ListAdminCommissionRatesQueryHandler
    : IRequestHandler<ListAdminCommissionRatesQuery, Result<IReadOnlyList<CompanyCommissionRateHistoryDto>>>
{
    private readonly ICompanyCommissionRateRepository _commissionRateRepository;

    public ListAdminCommissionRatesQueryHandler(ICompanyCommissionRateRepository commissionRateRepository) =>
        _commissionRateRepository = commissionRateRepository;

    public async Task<Result<IReadOnlyList<CompanyCommissionRateHistoryDto>>> Handle(
        ListAdminCommissionRatesQuery request,
        CancellationToken ct)
    {
        var rates = await _commissionRateRepository.ListByCompanyIdAsync(CompanyId.From(request.CompanyId), ct);
        var dtos = rates
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new CompanyCommissionRateHistoryDto(
                x.Id.Value,
                x.CommissionPercent,
                x.EffectiveFrom,
                x.EffectiveTo,
                x.Reason))
            .ToList();

        return Result<IReadOnlyList<CompanyCommissionRateHistoryDto>>.Success(dtos);
    }
}
