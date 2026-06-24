using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Commands.MarkSettlementPaid;

public sealed record MarkSettlementPaidCommand(long BatchId, string BankReference) : IRequest<Result>;

public sealed class MarkSettlementPaidCommandHandler : IRequestHandler<MarkSettlementPaidCommand, Result>
{
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly ISellerPayoutRepository _sellerPayoutRepository;
    private readonly ISellerLedgerRepository _sellerLedgerRepository;

    public MarkSettlementPaidCommandHandler(
        ISettlementBatchRepository settlementBatchRepository,
        ISellerPayoutRepository sellerPayoutRepository,
        ISellerLedgerRepository sellerLedgerRepository)
    {
        _settlementBatchRepository = settlementBatchRepository;
        _sellerPayoutRepository = sellerPayoutRepository;
        _sellerLedgerRepository = sellerLedgerRepository;
    }

    public async Task<Result> Handle(MarkSettlementPaidCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BankReference))
            return Result.Failure("Bank reference is required");

        var batch = await _settlementBatchRepository.GetByIdAsync(SettlementBatchId.From(request.BatchId), ct);
        if (batch is null)
            return Result.Failure("Settlement batch not found");
        if (batch.Status is SettlementBatchStatus.Paid)
            return Result.Success();
        if (batch.Status is not (SettlementBatchStatus.Ready or SettlementBatchStatus.Processing or SettlementBatchStatus.Failed))
            return Result.Failure("Batch cannot be marked paid in current status");

        var payout = await _sellerPayoutRepository.GetBySettlementBatchIdAsync(batch.Id, ct);
        if (payout is null)
        {
            payout = SellerPayout.CreatePending(
                SellerPayoutId.From(0),
                batch.CompanyId,
                batch.Id,
                batch.TotalAmount,
                batch.Currency,
                null,
                null);
            payout = await _sellerPayoutRepository.AddAsync(payout, ct);
        }

        payout.MarkPaid(request.BankReference.Trim());
        batch.MarkPaid(DateTime.UtcNow);

        var entries = (await _sellerLedgerRepository.ListByCompanyIdAsync(batch.CompanyId, ct))
            .Where(x => x.SettlementBatchId?.Value == batch.Id.Value)
            .ToList();
        foreach (var entry in entries)
        {
            entry.AssignToPayout(payout.Id);
            entry.MarkSettled(DateTime.UtcNow);
        }

        await _sellerPayoutRepository.UpdateAsync(payout, ct);
        await _settlementBatchRepository.UpdateAsync(batch, ct);
        if (entries.Count > 0)
            await _sellerLedgerRepository.UpdateRangeAsync(entries, ct);

        MarketplaceMetrics.SellerPayoutTotal.Add(1, [
            new KeyValuePair<string, object?>("status", SellerPayoutStatus.Paid.ToString()),
            new KeyValuePair<string, object?>("company_id", batch.CompanyId.Value.ToString()),
            new KeyValuePair<string, object?>("provider", "manual")
        ]);

        return Result.Success();
    }
}
