using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
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
        var result = await _sender.Send(new HandleLiqPayWebhookCommand(request.Data, request.Signature), ct);
        return result.IsSuccess ? Ok() : Unauthorized();
    }
}

public sealed record LiqPayWebhookRequest(string Data, string Signature);
