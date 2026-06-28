using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Options;
using Marketplace.Application.Finance.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class SellerPayoutProcessor
{
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly ISellerPayoutRepository _sellerPayoutRepository;
    private readonly ISellerLedgerRepository _sellerLedgerRepository;
    private readonly ICompanyLegalProfileRepository _companyLegalProfileRepository;
    private readonly ISellerPayoutPort _sellerPayoutPort;
    private readonly SettlementOptions _options;
    private readonly ILogger<SellerPayoutProcessor> _logger;

    public SellerPayoutProcessor(
        ISettlementBatchRepository settlementBatchRepository,
        ISellerPayoutRepository sellerPayoutRepository,
        ISellerLedgerRepository sellerLedgerRepository,
        ICompanyLegalProfileRepository companyLegalProfileRepository,
        ISellerPayoutPort sellerPayoutPort,
        IOptions<SettlementOptions> options,
        ILogger<SellerPayoutProcessor> logger)
    {
        _settlementBatchRepository = settlementBatchRepository;
        _sellerPayoutRepository = sellerPayoutRepository;
        _sellerLedgerRepository = sellerLedgerRepository;
        _companyLegalProfileRepository = companyLegalProfileRepository;
        _sellerPayoutPort = sellerPayoutPort;
        _options = options.Value;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task ProcessAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("finance-seller-payout", ProcessCoreAsync, ct);

    private async Task ProcessCoreAsync(CancellationToken ct)
    {
        if (!_options.AutoPayoutEnabled)
            return;

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "finance-seller-payout"));

        var readyBatches = await _settlementBatchRepository.ListByStatusAsync(SettlementBatchStatus.Ready, ct);
        foreach (var batch in readyBatches)
        {
            if (await _sellerPayoutRepository.GetBySettlementBatchIdAsync(batch.Id, ct) is not null)
                continue;

            var profile = await _companyLegalProfileRepository.GetByCompanyIdAsync(batch.CompanyId, ct);
            if (profile is null || string.IsNullOrWhiteSpace(profile.PayoutIban))
            {
                _logger.LogWarning("Skipping payout for company {CompanyId}: payout IBAN missing", batch.CompanyId.Value);
                continue;
            }

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
                    new KeyValuePair<string, object?>("company_id", batch.CompanyId.Value.ToString())
                ]);
            }
            else
            {
                payout.MarkFailed(result.Error ?? "Payout failed");
                batch.MarkFailed();
                await _sellerPayoutRepository.UpdateAsync(payout, ct);
                await _settlementBatchRepository.UpdateAsync(batch, ct);

                MarketplaceMetrics.SellerPayoutTotal.Add(1, [
                    new KeyValuePair<string, object?>("status", SellerPayoutStatus.Failed.ToString()),
                    new KeyValuePair<string, object?>("company_id", batch.CompanyId.Value.ToString())
                ]);
            }
        }

        MarketplaceMetrics.HangfireJobs.Add(1, [
            new KeyValuePair<string, object?>("job", "finance-seller-payout"),
            new KeyValuePair<string, object?>("status", "success")
        ]);
    }
}
