using Hangfire;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Jobs;

namespace Marketplace.Infrastructure.Notifications;

public sealed class HangfireAppNotificationRedispatcher : IAppNotificationRedispatcher
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireAppNotificationRedispatcher(IBackgroundJobClient jobs) => _jobs = jobs;

    public void EnqueueDispatch(
        string templateKey,
        Guid correlationId,
        int channels,
        int audience,
        Guid? targetUserId,
        Guid? targetCompanyId,
        string? payloadJson) =>
        _jobs.Enqueue<AppNotificationJobs>(job => job.DispatchAsync(
            templateKey,
            correlationId,
            channels,
            audience,
            targetUserId,
            targetCompanyId,
            payloadJson,
            CancellationToken.None));
}
