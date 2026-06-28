namespace Marketplace.Application.Notifications.Ports;

public interface INotificationChannel
{
    AppNotificationChannelKind Kind { get; }

    Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default);
}
