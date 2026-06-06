using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.ListMyShipments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Shipments")]
[Route("me/shipments")]
[Authorize]
public sealed class ShipmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ShippingOptions _shippingOptions;

    public ShipmentsController(ISender sender, IOptions<ShippingOptions> shippingOptions)
    {
        _sender = sender;
        _shippingOptions = shippingOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "list_shipments"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new ListMyShipmentsQuery(actorId), ct);
        return result.ToActionResult();
    }
}
