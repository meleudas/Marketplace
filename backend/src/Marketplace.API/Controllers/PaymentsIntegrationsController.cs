using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Application.Payments.Policies;
using Marketplace.Application.Common.Ports;
using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("PaymentsIntegrations")]
[Route("integrations/liqpay")]
[AllowAnonymous]
public sealed class PaymentsIntegrationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;
    private readonly PaymentWebhookAntiAbusePolicy _antiAbuse;

    public PaymentsIntegrationsController(
        ISender sender,
        IHttpIdempotencyStore idempotency,
        PaymentWebhookAntiAbusePolicy antiAbuse)
    {
        _sender = sender;
        _idempotency = idempotency;
        _antiAbuse = antiAbuse;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] LiqPayWebhookRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.WebhookLatencyMs, new KeyValuePair<string, object?>("provider", "liqpay"));
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var abuseCheck = await _antiAbuse.EvaluateClientIpAsync(clientIp, ct);
        if (!abuseCheck.Allowed)
        {
            MarketplaceMetrics.AbuseRejected.Add(1,
            [
                new KeyValuePair<string, object?>("domain", "payments"),
                new KeyValuePair<string, object?>("reason", abuseCheck.Reason ?? "webhook_ip_blocked"),
            ]);
            Response.Headers.RetryAfter = abuseCheck.RetryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, abuseCheck.Reason);
        }

        var scope = "liqpay-webhook";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(request.Data, request.Signature);
        // Provider-native idempotency: LiqPay callback payload is the dedup key.
        var idempotencyKey = requestHash;
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(24), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        var result = await _sender.Send(new HandleLiqPayWebhookCommand(request.Data, request.Signature, idempotencyKey), ct);
        if (result.IsSuccess)
            MarketplaceMetrics.WebhookOps.Add(1, [new KeyValuePair<string, object?>("provider", "liqpay"), new KeyValuePair<string, object?>("status", "success")]);
        else
        {
            MarketplaceMetrics.WebhookErrors.Add(1, [new KeyValuePair<string, object?>("provider", "liqpay")]);
            if (string.Equals(result.Error, "Invalid LiqPay signature", StringComparison.Ordinal))
            {
                await _antiAbuse.RecordFailedSignatureAsync(clientIp, ct);
                MarketplaceMetrics.AbuseRejected.Add(1,
                [
                    new KeyValuePair<string, object?>("domain", "payments"),
                    new KeyValuePair<string, object?>("reason", "invalid_signature"),
                ]);
            }
        }
        IActionResult actionResult = result.IsSuccess ? Ok() : Unauthorized();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }
}

public sealed record LiqPayWebhookRequest(string Data, string Signature);
