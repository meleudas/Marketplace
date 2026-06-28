using Hangfire;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class HangfireAppNotificationScheduler : IAppNotificationScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireAppNotificationScheduler(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
    {
        _ = ct;
        _jobs.Enqueue<AppNotificationJobs>(x => x.DispatchAsync(
            request.TemplateKey,
            request.CorrelationId,
            (int)request.Channels,
            (int)request.Audience,
            request.TargetUserId,
            request.TargetCompanyId,
            request.PayloadJson,
            default));
        return Task.CompletedTask;
    }
}
