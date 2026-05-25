namespace Marketplace.Application.Notifications.Ports;

public sealed record PushDeliveryRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string Payload);

public interface IPushDeliveryClient
{
    Task SendAsync(PushDeliveryRequest request, CancellationToken ct = default);
}
