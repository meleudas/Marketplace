using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class OutboxDispatcherJobs
{
    private readonly IOutboxWriter _outbox;
    private readonly IOutboxEventProcessor _processor;
    private readonly OutboxOptions _options;

    public OutboxDispatcherJobs(
        IOutboxWriter outbox,
        IOutboxEventProcessor processor,
        IOptions<OutboxOptions> options)
    {
        _outbox = outbox;
        _processor = processor;
        _options = options.Value;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task DispatchPendingAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("outbox-dispatch-pending", DispatchPendingCoreAsync, ct);

    private async Task DispatchPendingCoreAsync(CancellationToken ct)
    {
        using var span = MarketplaceTelemetry.StartActivity("outbox.dispatch");
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "outbox-dispatch"));

        var maxAttempts = Math.Clamp(_options.MaxAttempts, 1, 50);
        var batchSize = Math.Clamp(_options.BatchSize, 1, 500);
        var now = DateTime.UtcNow;
        var batch = await _outbox.ListPendingAsync(batchSize, now, ct);
        foreach (var message in batch)
        {
            try
            {
                await _processor.ProcessAsync(message, ct);
                await _outbox.MarkProcessedAsync(message.Id, ct);
                MarketplaceMetrics.OutboxDispatches.Add(1,
                [
                    new KeyValuePair<string, object?>("status", "processed"),
                    new KeyValuePair<string, object?>("event_type", message.EventType)
                ]);
            }
            catch (PermanentOutboxException ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch")]);
                await _outbox.MarkDeadLetterAsync(message.Id, ex.Message, "permanent", ct);
                MarketplaceMetrics.OutboxDispatchErrors.Add(1,
                [
                    new KeyValuePair<string, object?>("category", "permanent"),
                    new KeyValuePair<string, object?>("event_type", message.EventType)
                ]);
                MarketplaceMetrics.OutboxDeadLetters.Add(1,
                [
                    new KeyValuePair<string, object?>("category", "permanent"),
                    new KeyValuePair<string, object?>("event_type", message.EventType)
                ]);
            }
            catch (Exception ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch")]);
                if (message.Attempts + 1 >= maxAttempts)
                {
                    await _outbox.MarkDeadLetterAsync(message.Id, ex.Message, "exhausted", ct);
                    MarketplaceMetrics.OutboxDispatchErrors.Add(1,
                    [
                        new KeyValuePair<string, object?>("category", "exhausted"),
                        new KeyValuePair<string, object?>("event_type", message.EventType)
                    ]);
                    MarketplaceMetrics.OutboxDeadLetters.Add(1,
                    [
                        new KeyValuePair<string, object?>("category", "exhausted"),
                        new KeyValuePair<string, object?>("event_type", message.EventType)
                    ]);
                    continue;
                }

                var nextAttempt = RetryBackoffCalculator.ComputeNextAttemptUtc(
                    message.Attempts + 1,
                    _options.BaseBackoffMinutes,
                    _options.MaxBackoffMinutes,
                    now);
                await _outbox.MarkFailedAsync(message.Id, ex.Message, nextAttempt, ct);
                MarketplaceMetrics.OutboxDispatchErrors.Add(1,
                [
                    new KeyValuePair<string, object?>("category", "transient"),
                    new KeyValuePair<string, object?>("event_type", message.EventType)
                ]);
            }
        }

        MarketplaceMetrics.HangfireJobs.Add(1,
        [
            new KeyValuePair<string, object?>("job", "outbox-dispatch"),
            new KeyValuePair<string, object?>("status", "success")
        ]);
    }
}
