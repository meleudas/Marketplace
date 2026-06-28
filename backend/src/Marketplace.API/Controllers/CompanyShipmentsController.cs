using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Commands.CreateShipment;
using Marketplace.Application.Shipping.Queries.GetShipmentById;
using Marketplace.Application.Shipping.Queries.ListOrderShipments;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Common.Ports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("CompanyShipments")]
[Authorize]
public sealed class CompanyShipmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;
    private readonly ShippingOptions _shippingOptions;

    public CompanyShipmentsController(
        ISender sender,
        IHttpIdempotencyStore idempotency,
        IOptions<ShippingOptions> shippingOptions)
    {
        _sender = sender;
        _idempotency = idempotency;
        _shippingOptions = shippingOptions.Value;
    }

    [HttpPost("companies/{companyId:guid}/orders/{orderId:long}/shipments")]
    public async Task<IActionResult> Create(
        Guid companyId,
        long orderId,
        [FromBody] CreateShipmentRequest body,
        CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var scope = $"shipment-create:{companyId:N}:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(
            companyId.ToString("N"), orderId.ToString(), actorId.ToString("N"), body.TrackingNumber ?? string.Empty);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "company_create_shipment"));
        var result = await _sender.Send(new CreateShipmentCommand(
            orderId,
            companyId,
            actorId,
            User.IsInRole("Admin"),
            body.WarehouseId,
            body.Lines,
            body.TrackingNumber), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }

    [HttpGet("companies/{companyId:guid}/orders/{orderId:long}/shipments")]
    public async Task<IActionResult> ListForOrder(Guid companyId, long orderId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "company_list_order_shipments"));
        var result = await _sender.Send(new ListOrderShipmentsQuery(orderId, actorId, User.IsInRole("Admin"), true, companyId), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/shipments/{shipmentId:long}")]
    public async Task<IActionResult> GetById(Guid companyId, long shipmentId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "company_get_shipment"));
        var result = await _sender.Send(new GetShipmentByIdQuery(shipmentId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }
}

public sealed record CreateShipmentRequest(long? WarehouseId, IReadOnlyList<CreateShipmentLineDto> Lines, string? TrackingNumber);
