using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Commands.ApproveSettlementPayout;

public sealed record ApproveSettlementPayoutCommand(long BatchId) : IRequest<Result>;

public sealed class ApproveSettlementPayoutCommandHandler : IRequestHandler<ApproveSettlementPayoutCommand, Result>
{
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly ISellerPayoutRepository _sellerPayoutRepository;
    private readonly ISellerLedgerRepository _sellerLedgerRepository;
    private readonly ICompanyLegalProfileRepository _companyLegalProfileRepository;
    private readonly ISellerPayoutPort _sellerPayoutPort;

    public ApproveSettlementPayoutCommandHandler(
        ISettlementBatchRepository settlementBatchRepository,
        ISellerPayoutRepository sellerPayoutRepository,
        ISellerLedgerRepository sellerLedgerRepository,
        ICompanyLegalProfileRepository companyLegalProfileRepository,
        ISellerPayoutPort sellerPayoutPort)
    {
        _settlementBatchRepository = settlementBatchRepository;
        _sellerPayoutRepository = sellerPayoutRepository;
        _sellerLedgerRepository = sellerLedgerRepository;
        _companyLegalProfileRepository = companyLegalProfileRepository;
        _sellerPayoutPort = sellerPayoutPort;
    }

    public async Task<Result> Handle(ApproveSettlementPayoutCommand request, CancellationToken ct)
    {
        var batch = await _settlementBatchRepository.GetByIdAsync(SettlementBatchId.From(request.BatchId), ct);
        if (batch is null)
            return Result.Failure("Settlement batch not found");
        if (batch.Status != SettlementBatchStatus.Ready)
            return Result.Failure("Only ready batches can be approved for payout");
        if (await _sellerPayoutRepository.GetBySettlementBatchIdAsync(batch.Id, ct) is not null)
            return Result.Failure("Payout already exists for this batch");

        var profile = await _companyLegalProfileRepository.GetByCompanyIdAsync(batch.CompanyId, ct);
        if (profile is null || string.IsNullOrWhiteSpace(profile.PayoutIban))
            return Result.Failure("Company payout IBAN is not configured");

        batch.MarkProcessing();
        await _settlementBatchRepository.UpdateAsync(batch, ct);

        var payout = SellerPayout.CreatePending(
            SellerPayoutId.From(0),
            batch.CompanyId,
            batch.Id,
            batch.TotalAmount,
            batch.Currency,
            profile.PayoutIban,
            profile.PayoutRecipientName);
        payout = await _sellerPayoutRepository.AddAsync(payout, ct);

        var result = await _sellerPayoutPort.InitiatePayoutAsync(
            new SellerPayoutRequest(
                batch.CompanyId.Value,
                batch.Id.Value,
                batch.TotalAmount,
                batch.Currency,
                profile.PayoutIban,
                profile.PayoutRecipientName,
                profile.PayoutProviderAccountId),
            ct);

        if (result.IsSuccess)
        {
            payout.MarkPaid(result.ProviderReference);
            batch.MarkPaid(DateTime.UtcNow);
            await FinalizePayoutAsync(batch, payout, ct);
            MarketplaceMetrics.SellerPayoutTotal.Add(1, [
                new KeyValuePair<string, object?>("status", SellerPayoutStatus.Paid.ToString()),
                new KeyValuePair<string, object?>("company_id", batch.CompanyId.Value.ToString())
            ]);
            return Result.Success();
        }

        payout.MarkFailed(result.Error ?? "Payout failed");
        batch.MarkFailed();
        await _sellerPayoutRepository.UpdateAsync(payout, ct);
        await _settlementBatchRepository.UpdateAsync(batch, ct);
        MarketplaceMetrics.SellerPayoutTotal.Add(1, [
            new KeyValuePair<string, object?>("status", SellerPayoutStatus.Failed.ToString()),
            new KeyValuePair<string, object?>("company_id", batch.CompanyId.Value.ToString())
        ]);
        return Result.Failure(result.Error ?? "Payout failed");
    }

    private async Task FinalizePayoutAsync(SettlementBatch batch, SellerPayout payout, CancellationToken ct)
    {
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
    }
}
