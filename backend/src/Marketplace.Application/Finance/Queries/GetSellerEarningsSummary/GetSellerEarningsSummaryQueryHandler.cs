using Marketplace.Application.Finance.Authorization;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Queries.GetSellerEarningsSummary;

public sealed class GetSellerEarningsSummaryQueryHandler
    : IRequestHandler<GetSellerEarningsSummaryQuery, Result<SellerEarningsSummaryDto>>
{
    private readonly IFinanceAccessService _access;
    private readonly ISellerLedgerRepository _sellerLedgerRepository;

    public GetSellerEarningsSummaryQueryHandler(
        IFinanceAccessService access,
        ISellerLedgerRepository sellerLedgerRepository)
    {
        _access = access;
        _sellerLedgerRepository = sellerLedgerRepository;
    }

    public async Task<Result<SellerEarningsSummaryDto>> Handle(GetSellerEarningsSummaryQuery request, CancellationToken ct)
    {
        if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, FinancePermission.Read, ct))
            return Result<SellerEarningsSummaryDto>.Failure("Forbidden");

        var companyId = CompanyId.From(request.CompanyId);
        var entries = await _sellerLedgerRepository.ListByCompanyIdAsync(companyId, ct);
        if (request.FromUtc is not null)
            entries = entries.Where(x => x.CreatedAt >= request.FromUtc.Value).ToList();
        if (request.ToUtc is not null)
            entries = entries.Where(x => x.CreatedAt <= request.ToUtc.Value).ToList();
        decimal pending = 0m;
        decimal available = 0m;
        decimal settled = 0m;
        decimal platformFees = 0m;
        var currency = "UAH";

        foreach (var entry in entries)
        {
            currency = entry.Currency;
            if (entry.EntryType == SellerLedgerEntryType.PlatformFee)
            {
                platformFees += entry.Amount;
                continue;
            }

            if (entry.EntryType is not (SellerLedgerEntryType.Sale or SellerLedgerEntryType.Refund or SellerLedgerEntryType.Adjustment))
                continue;

            switch (entry.Status)
            {
                case SellerLedgerEntryStatus.Pending:
                    pending += entry.Amount;
                    break;
                case SellerLedgerEntryStatus.Available:
                    available += entry.Amount;
                    break;
                case SellerLedgerEntryStatus.Settled:
                    settled += entry.Amount;
                    break;
            }
        }

        return Result<SellerEarningsSummaryDto>.Success(new SellerEarningsSummaryDto(
            request.CompanyId,
            pending,
            available,
            settled,
            platformFees,
            currency));
    }
}
