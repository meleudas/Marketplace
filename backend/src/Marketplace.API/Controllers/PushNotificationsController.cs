using Marketplace.API.Extensions;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
public sealed class PushNotificationsController : ControllerBase
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IOptionsMonitor<WebPushOptions> _webPush;

    public PushNotificationsController(
        IPushSubscriptionRepository subscriptions,
        IOptionsMonitor<WebPushOptions> webPush)
    {
        _subscriptions = subscriptions;
        _webPush = webPush;
    }

    [HttpGet("web-push/vapid-public-key")]
    [AllowAnonymous]
    public ActionResult<VapidPublicKeyResponse> GetVapidPublicKey()
    {
        var opt = _webPush.CurrentValue;
        return Ok(new VapidPublicKeyResponse(opt.PublicKey ?? string.Empty, opt.Subject ?? string.Empty));
    }

    [HttpPost("me/web-push/subscriptions")]
    [Authorize]
    public async Task<IActionResult> RegisterSubscription(
        [FromBody] RegisterPushSubscriptionRequest body,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(body.Endpoint) ||
            string.IsNullOrWhiteSpace(body.P256dh) ||
            string.IsNullOrWhiteSpace(body.Auth))
            return BadRequest(new { detail = "endpoint, p256dh and auth are required." });

        var flags = PushSubscriptionAudienceFlags.None;
        if (body.IncludeUserChannel)
            flags |= PushSubscriptionAudienceFlags.UserWebPush;

        var isAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator");
        if (body.IncludeAdminChannel && isAdminOrModerator)
            flags |= PushSubscriptionAudienceFlags.AdminWebPush;

        if (flags == PushSubscriptionAudienceFlags.None)
            return BadRequest(new { detail = "Select at least one channel (includeUserChannel or includeAdminChannel for admins/moderators)." });

        var ua = Request.Headers.UserAgent.ToString();
        var userAgent = string.IsNullOrWhiteSpace(ua) ? null : ua[..Math.Min(ua.Length, 512)];

        await _subscriptions.UpsertAsync(userId, body.Endpoint.Trim(), body.P256dh.Trim(), body.Auth.Trim(), flags,
            userAgent, ct);
        return NoContent();
    }

    [HttpDelete("me/web-push/subscriptions")]
    [Authorize]
    public async Task<IActionResult> DeleteSubscription([FromQuery] string endpoint, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(endpoint))
            return BadRequest(new { detail = "endpoint query parameter is required." });

        await _subscriptions.DeleteByUserAndEndpointAsync(userId, endpoint.Trim(), ct);
        return NoContent();
    }

    public sealed record VapidPublicKeyResponse(string PublicKey, string Subject);

    public sealed class RegisterPushSubscriptionRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public bool IncludeUserChannel { get; set; } = true;
        public bool IncludeAdminChannel { get; set; }
    }
}
