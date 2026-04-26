using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Marketplace.Infrastructure.Observability;

public static class MarketplaceMetrics
{
    public const string MeterName = "Marketplace.Observability";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("cache_hits_total");
    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("cache_misses_total");
    public static readonly Counter<long> CacheErrors = Meter.CreateCounter<long>("cache_errors_total");
    public static readonly Histogram<double> CacheLatencyMs = Meter.CreateHistogram<double>("cache_latency_ms");

    public static readonly Counter<long> PaymentOps = Meter.CreateCounter<long>("payment_operations_total");
    public static readonly Counter<long> PaymentErrors = Meter.CreateCounter<long>("payment_errors_total");
    public static readonly Histogram<double> PaymentLatencyMs = Meter.CreateHistogram<double>("payment_latency_ms");

    public static readonly Counter<long> WebhookOps = Meter.CreateCounter<long>("webhook_operations_total");
    public static readonly Counter<long> WebhookErrors = Meter.CreateCounter<long>("webhook_errors_total");
    public static readonly Histogram<double> WebhookLatencyMs = Meter.CreateHistogram<double>("webhook_latency_ms");

    public static readonly Counter<long> HangfireJobs = Meter.CreateCounter<long>("hangfire_jobs_total");
    public static readonly Counter<long> HangfireJobErrors = Meter.CreateCounter<long>("hangfire_job_errors_total");
    public static readonly Histogram<double> HangfireJobLatencyMs = Meter.CreateHistogram<double>("hangfire_job_latency_ms");

    public static IDisposable StartTimer(Histogram<double> histogram, params KeyValuePair<string, object?>[] tags)
        => new TimerScope(histogram, tags);

    private sealed class TimerScope : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object?>[] _tags;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public TimerScope(Histogram<double> histogram, KeyValuePair<string, object?>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
        }

        public void Dispose()
        {
            _sw.Stop();
            _histogram.Record(_sw.Elapsed.TotalMilliseconds, _tags);
        }
    }
}
