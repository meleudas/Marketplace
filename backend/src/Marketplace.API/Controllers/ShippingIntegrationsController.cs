using System.Security.Cryptography;
using System.Text;
using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("integrations/shipping/novaposhta")]
[AllowAnonymous]
public sealed class ShippingIntegrationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ShippingOptions _shippingOptions;
    private readonly bool _novaPoshtaEnabled;

    public ShippingIntegrationsController(
        ISender sender,
        IOptions<ShippingOptions> shippingOptions,
        IConfiguration configuration)
    {
        _sender = sender;
        _shippingOptions = shippingOptions.Value;
        _novaPoshtaEnabled = configuration.GetValue<bool>("NovaPoshta:Enabled");
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] object payload, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled || !_shippingOptions.NovaPoshtaEnabled || !_novaPoshtaEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "novaposhta_webhook"));
        var rawPayload = payload?.ToString() ?? "{}";
        var eventKey = Request.Headers["X-NovaPoshta-Event-Id"].ToString();
        if (string.IsNullOrWhiteSpace(eventKey))
            eventKey = Guid.NewGuid().ToString("N");
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload)));

        var result = await _sender.Send(new HandleNovaPoshtaWebhookCommand(eventKey, payloadHash, rawPayload), ct);
        return result.ToActionResult();
    }
}
