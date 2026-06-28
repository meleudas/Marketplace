using System.Diagnostics;

namespace Marketplace.Application.Common.Observability;

public static class MarketplaceTelemetry
{
    public const string ActivitySourceName = "Marketplace.Telemetry";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => ActivitySource.StartActivity(name, kind);

    public static async Task RunJobAsync(string jobName, Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        using var activity = StartActivity($"job.{jobName}");
        activity?.SetTag("job", jobName);
        await action(ct).ConfigureAwait(false);
    }
}
