using System.Net;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class LibWebPushDeliveryClient : IPushDeliveryClient
{
    private readonly PushServiceClient _client;
    private readonly IOptionsMonitor<WebPushOptions> _options;

    public LibWebPushDeliveryClient(PushServiceClient client, IOptionsMonitor<WebPushOptions> options)
    {
        _client = client;
        _options = options;
    }

    public async Task SendAsync(PushDeliveryRequest request, CancellationToken ct = default)
    {
        var opt = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(opt.PublicKey) || string.IsNullOrWhiteSpace(opt.PrivateKey))
            throw new InvalidOperationException("WebPush VAPID keys are not configured.");

        var subscription = new PushSubscription { Endpoint = request.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, request.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, request.Auth);

        using var vapid = new VapidAuthentication(opt.PublicKey, opt.PrivateKey) { Subject = opt.Subject };
        var message = new PushMessage(request.Payload);

        await _client.RequestPushMessageDeliveryAsync(
            subscription,
            message,
            vapid,
            VapidAuthenticationScheme.Vapid,
            ct);
    }

    public static bool IsSubscriptionGone(Exception ex) =>
        ex is PushServiceClientException p && p.StatusCode == HttpStatusCode.Gone;
}
