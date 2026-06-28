namespace Marketplace.Application.Notifications.Ports;

public interface IAppNotificationScheduler
{
    Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default);
}
