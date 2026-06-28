using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.GetShipmentById;
using Marketplace.Application.Shipping.Queries.ListMyShipments;
using Marketplace.Application.Shipping.Queries.ListOrderShipments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Shipments")]
[Route("me")]
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

    [HttpGet("shipments")]
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

    [HttpGet("shipments/{shipmentId:long}")]
    public async Task<IActionResult> GetById(long shipmentId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "get_shipment"));
        var result = await _sender.Send(new GetShipmentByIdQuery(shipmentId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpGet("orders/{orderId:long}/shipments")]
    public async Task<IActionResult> ListForOrder(long orderId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "list_order_shipments"));
        var result = await _sender.Send(new ListOrderShipmentsQuery(orderId, actorId, User.IsInRole("Admin"), false, null), ct);
        return result.ToActionResult();
    }
}
