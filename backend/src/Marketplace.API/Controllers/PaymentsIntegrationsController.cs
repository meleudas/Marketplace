using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
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

    public PaymentsIntegrationsController(ISender sender) => _sender = sender;

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] LiqPayWebhookRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.WebhookLatencyMs, new KeyValuePair<string, object?>("provider", "liqpay"));
        var result = await _sender.Send(new HandleLiqPayWebhookCommand(request.Data, request.Signature), ct);
        if (result.IsSuccess)
            MarketplaceMetrics.WebhookOps.Add(1, [new KeyValuePair<string, object?>("provider", "liqpay"), new KeyValuePair<string, object?>("status", "success")]);
        else
            MarketplaceMetrics.WebhookErrors.Add(1, [new KeyValuePair<string, object?>("provider", "liqpay")]);
        return result.IsSuccess ? Ok() : Unauthorized();
    }
}

public sealed record LiqPayWebhookRequest(string Data, string Signature);
