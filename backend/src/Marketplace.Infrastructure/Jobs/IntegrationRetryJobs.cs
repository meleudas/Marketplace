using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class IntegrationRetryJobs
{
    private readonly IIntegrationRetryStore _store;
    private readonly IntegrationRetryProcessor _processor;
    private readonly IntegrationRetryOptions _options;

    public IntegrationRetryJobs(
        IIntegrationRetryStore store,
        IntegrationRetryProcessor processor,
        IOptions<IntegrationRetryOptions> options)
    {
        _store = store;
        _processor = processor;
        _options = options.Value;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task DispatchDueAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("integration-retry-dispatch", DispatchDueCoreAsync, ct);

    private async Task DispatchDueCoreAsync(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "integration-retry-dispatch"));

        var maxAttempts = Math.Clamp(_options.MaxAttempts, 1, 50);
        var batchSize = Math.Clamp(_options.BatchSize, 1, 200);
        var now = DateTime.UtcNow;
        var batch = await _store.ListDueAsync(batchSize, now, ct);

        foreach (var entry in batch)
        {
            try
            {
                MarketplaceMetrics.IntegrationRetryAttempts.Add(1, [new KeyValuePair<string, object?>("kind", entry.Kind)]);
                var resolved = await _processor.TryProcessAsync(entry, ct);
                if (resolved)
                    await _store.MarkResolvedAsync(entry.Id, ct);
            }
            catch (Exception ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "integration-retry-dispatch")]);
                if (entry.Attempts + 1 >= maxAttempts)
                {
                    await _store.MarkDeadLetterAsync(entry.Id, ex.Message, "exhausted", ct);
                    MarketplaceMetrics.IntegrationRetryDeadLetters.Add(1, [new KeyValuePair<string, object?>("kind", entry.Kind)]);
                    continue;
                }

                var nextAttempt = RetryBackoffCalculator.ComputeNextAttemptUtc(
                    entry.Attempts + 1,
                    _options.BaseBackoffMinutes,
                    _options.MaxBackoffMinutes,
                    now);
                await _store.MarkFailedAsync(entry.Id, ex.Message, nextAttempt, ct);
            }
        }

        MarketplaceMetrics.HangfireJobs.Add(1,
        [
            new KeyValuePair<string, object?>("job", "integration-retry-dispatch"),
            new KeyValuePair<string, object?>("status", "success")
        ]);
    }
}
