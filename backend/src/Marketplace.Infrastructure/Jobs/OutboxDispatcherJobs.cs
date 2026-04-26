using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Observability;

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
            catch (Exception ex)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch")]);
                var nextDelayMinutes = Math.Min(60, Math.Pow(2, Math.Min(message.Attempts + 1, 6)));
                var nextAttempt = now.AddMinutes(nextDelayMinutes);
                var error = message.Attempts + 1 >= MaxAttempts
                    ? $"poison:{ex.Message}"
                    : ex.Message;
                await _outbox.MarkFailedAsync(message.Id, error, nextAttempt, ct);
            }
        }
        MarketplaceMetrics.HangfireJobs.Add(1, [new KeyValuePair<string, object?>("job", "outbox-dispatch"), new KeyValuePair<string, object?>("status", "success")]);
    }
}
