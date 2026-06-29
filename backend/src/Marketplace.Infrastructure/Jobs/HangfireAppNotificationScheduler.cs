using Hangfire;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Policies;
using Marketplace.Application.Notifications.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class HangfireAppNotificationScheduler : IAppNotificationScheduler
{
    private readonly IBackgroundJobClient _jobs;
    private readonly NotificationDispatchAntiAbusePolicy _antiAbuse;

    public HangfireAppNotificationScheduler(
        IBackgroundJobClient jobs,
        NotificationDispatchAntiAbusePolicy antiAbuse)
    {
        _jobs = jobs;
        _antiAbuse = antiAbuse;
    }

    public async Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
    {
        var abuse = await _antiAbuse.EvaluateScheduleAsync(request, ct);
        if (!abuse.Allowed)
        {
            MarketplaceMetrics.AbuseRejected.Add(1,
            [
                new KeyValuePair<string, object?>("domain", "notifications"),
                new KeyValuePair<string, object?>("reason", abuse.Reason ?? "dispatch_burst"),
            ]);
            return;
        }

        _jobs.Enqueue<AppNotificationJobs>(x => x.DispatchAsync(
            request.TemplateKey,
            request.CorrelationId,
            (int)request.Channels,
            (int)request.Audience,
            request.TargetUserId,
            request.TargetCompanyId,
            request.PayloadJson,
            default));
    }
}
