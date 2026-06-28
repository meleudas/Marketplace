using System.Text.Json;
using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Support.Commands.SyncTicketFromHelpdeskWebhook;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("SupportIntegrations")]
[Route("integrations/support/helpdesk")]
[AllowAnonymous]
public sealed class SupportIntegrationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SupportOptions _options;
    private readonly HelpdeskWebhookSignatureValidator _signatureValidator;

    public SupportIntegrationsController(
        ISender sender,
        IOptions<SupportOptions> options,
        HelpdeskWebhookSignatureValidator signatureValidator)
    {
        _sender = sender;
        _options = options.Value;
        _signatureValidator = signatureValidator;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.HelpdeskWebhookEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var rawPayload = payload.GetRawText();
        var signature = Request.Headers["X-Helpdesk-Signature"].ToString();
        if (!_signatureValidator.IsValid(rawPayload, signature))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.SupportTicketLatencyMs,
            new KeyValuePair<string, object?>("operation", "helpdesk_webhook"));

        var eventId = payload.TryGetProperty("eventId", out var eventIdProp)
            ? eventIdProp.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(eventId))
            eventId = Guid.NewGuid().ToString("N");

        var result = await _sender.Send(new SyncTicketFromHelpdeskWebhookCommand(eventId, rawPayload), ct);
        if (result.IsSuccess)
            MarketplaceMetrics.SupportTicketsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "helpdesk_webhook")]);
        else
            MarketplaceMetrics.SupportTicketErrorsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "helpdesk_webhook")]);

        return result.ToActionResult();
    }
}
