using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Options;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class SettlementBatchJob
{
    private readonly ISellerLedgerRepository _sellerLedgerRepository;
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly SettlementOptions _options;

    public SettlementBatchJob(
        ISellerLedgerRepository sellerLedgerRepository,
        ISettlementBatchRepository settlementBatchRepository,
        IOptions<SettlementOptions> options)
    {
        _sellerLedgerRepository = sellerLedgerRepository;
        _settlementBatchRepository = settlementBatchRepository;
        _options = options.Value;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("finance-settlement-batch", RunCoreAsync, ct);

    private async Task RunCoreAsync(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "finance-settlement-batch"));

        var now = DateTime.UtcNow;
        var companyIds = await _sellerLedgerRepository.ListCompanyIdsWithPendingSaleEntriesAsync(now, ct);

        foreach (var companyId in companyIds)
        {
            var pendingEntries = await _sellerLedgerRepository.ListAvailableForSettlementAsync(companyId, now, ct);
            if (pendingEntries.Count == 0)
                continue;

            foreach (var entry in pendingEntries)
                entry.MarkAvailable(now);

            await _sellerLedgerRepository.UpdateRangeAsync(pendingEntries, ct);

            var availableEntries = (await _sellerLedgerRepository.ListByCompanyIdAsync(companyId, ct))
                .Where(x => x.Status == SellerLedgerEntryStatus.Available
                    && x.EntryType == SellerLedgerEntryType.Sale
                    && x.SettlementBatchId is null)
                .ToList();

            if (availableEntries.Count == 0)
                continue;

            var batch = await _settlementBatchRepository.GetOpenByCompanyIdAsync(companyId, ct);
            if (batch is null)
            {
                var periodStart = now.Date.AddDays(-_options.PeriodDays);
                batch = SettlementBatch.CreateOpen(
                    SettlementBatchId.From(0),
                    companyId,
                    periodStart,
                    now,
                    availableEntries[0].Currency);
                batch = await _settlementBatchRepository.AddAsync(batch, ct);
            }

            decimal total = 0m;
            foreach (var entry in availableEntries)
            {
                total += entry.Amount;
                entry.AssignToSettlementBatch(batch.Id);
            }

            batch.AddAmount(total);
            if (batch.PeriodEndUtc <= now || total > 0)
                batch.MarkReady(now);

            await _settlementBatchRepository.UpdateAsync(batch, ct);
            await _sellerLedgerRepository.UpdateRangeAsync(availableEntries, ct);

            MarketplaceMetrics.SettlementBatchTotal.Add(1, [
                new KeyValuePair<string, object?>("status", batch.Status.ToString()),
                new KeyValuePair<string, object?>("company_id", companyId.Value.ToString())
            ]);
        }

        MarketplaceMetrics.HangfireJobs.Add(1, [
            new KeyValuePair<string, object?>("job", "finance-settlement-batch"),
            new KeyValuePair<string, object?>("status", "success")
        ]);
    }
}
