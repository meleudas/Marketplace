using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Observability;
using Hangfire;

namespace Marketplace.Infrastructure.Jobs;

public sealed class OutboxDispatcherJobs
{
    private readonly IOutboxWriter _outbox;
    private readonly IOutboxEventProcessor _processor;
    private const int MaxAttempts = 10;

    public OutboxDispatcherJobs(IOutboxWriter outbox, IOutboxEventProcessor processor)
    {
        _outbox = outbox;
        _processor = processor;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task DispatchPendingAsync(CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.HangfireJobLatencyMs, new KeyValuePair<string, object?>("job", "outbox-dispatch"));
        var now = DateTime.UtcNow;
        var batch = await _outbox.ListPendingAsync(100, now, ct);
        foreach (var message in batch)
        {
            try
            {
                await _processor.ProcessAsync(message, ct);
                await _outbox.MarkProcessedAsync(message.Id, ct);
            }
            catch (PermanentOutboxException ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch")]);
                await _outbox.MarkDeadLetterAsync(message.Id, ex.Message, "permanent", ct);
            }
            catch (Exception ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch")]);
                if (message.Attempts + 1 >= MaxAttempts)
                {
                    await _outbox.MarkDeadLetterAsync(message.Id, ex.Message, "exhausted", ct);
                    continue;
                }

                var nextDelayMinutes = Math.Min(60, Math.Pow(2, Math.Min(message.Attempts + 1, 6)));
                var jitterSeconds = Random.Shared.Next(1, 15);
                var nextAttempt = now.AddMinutes(nextDelayMinutes).AddSeconds(jitterSeconds);
                await _outbox.MarkFailedAsync(message.Id, ex.Message, nextAttempt, ct);
            }
        }
        MarketplaceMetrics.HangfireJobs.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch"), new KeyValuePair<string, object?>("status", "success")]);
    }
}
