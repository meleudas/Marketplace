using Marketplace.Application.Finance.Authorization;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Queries.ListCompanySettlements;

public sealed record ListCompanySettlementsQuery(Guid CompanyId, Guid ActorUserId, bool IsActorAdmin)
    : IRequest<Result<IReadOnlyList<SettlementBatchDto>>>;

public sealed class ListCompanySettlementsQueryHandler
    : IRequestHandler<ListCompanySettlementsQuery, Result<IReadOnlyList<SettlementBatchDto>>>
{
    private readonly IFinanceAccessService _access;
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly ISellerPayoutRepository _sellerPayoutRepository;

    public ListCompanySettlementsQueryHandler(
        IFinanceAccessService access,
        ISettlementBatchRepository settlementBatchRepository,
        ISellerPayoutRepository sellerPayoutRepository)
    {
        _access = access;
        _settlementBatchRepository = settlementBatchRepository;
        _sellerPayoutRepository = sellerPayoutRepository;
    }

    public async Task<Result<IReadOnlyList<SettlementBatchDto>>> Handle(ListCompanySettlementsQuery request, CancellationToken ct)
    {
        if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, FinancePermission.Read, ct))
            return Result<IReadOnlyList<SettlementBatchDto>>.Failure("Forbidden");

        var companyId = CompanyId.From(request.CompanyId);
        var batches = await _settlementBatchRepository.ListByCompanyIdAsync(companyId, ct);
        var dtos = new List<SettlementBatchDto>();

        foreach (var batch in batches)
        {
            var payout = await _sellerPayoutRepository.GetBySettlementBatchIdAsync(batch.Id, ct);
            dtos.Add(ToDto(batch, payout));
        }

        return Result<IReadOnlyList<SettlementBatchDto>>.Success(dtos);
    }

    internal static SettlementBatchDto ToDto(
        Domain.Finance.Entities.SettlementBatch batch,
        Domain.Finance.Entities.SellerPayout? payout) =>
        new(
            batch.Id.Value,
            batch.CompanyId.Value,
            batch.PeriodStartUtc,
            batch.PeriodEndUtc,
            batch.Status.ToString(),
            batch.TotalAmount,
            batch.Currency,
            batch.ClosedAtUtc,
            batch.PaidAtUtc,
            payout is null
                ? null
                : new SellerPayoutDto(
                    payout.Id.Value,
                    payout.SettlementBatchId.Value,
                    payout.Status.ToString(),
                    payout.Amount,
                    payout.Currency,
                    payout.ProviderReference,
                    payout.FailureReason,
                    payout.CompletedAtUtc));
}
