using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Application.Common.Ports;
using Marketplace.API.Extensions;
using Marketplace.Infrastructure.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("integrations/liqpay")]
[AllowAnonymous]
public sealed class PaymentsIntegrationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;

    public PaymentsIntegrationsController(ISender sender, IHttpIdempotencyStore idempotency)
    {
        _sender = sender;
        _idempotency = idempotency;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] LiqPayWebhookRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.WebhookLatencyMs, new KeyValuePair<string, object?>("provider", "liqpay"));
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var scope = "liqpay-webhook";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(request.Data, request.Signature);
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
            MarketplaceMetrics.WebhookErrors.Add(1, [new KeyValuePair<string, object?>("provider", "liqpay")]);
        IActionResult actionResult = result.IsSuccess ? Ok() : Unauthorized();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }
}

public sealed record LiqPayWebhookRequest(string Data, string Signature);
