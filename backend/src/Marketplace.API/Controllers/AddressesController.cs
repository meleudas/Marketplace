using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Commands.CreateUserAddress;
using Marketplace.Application.Shipping.Commands.DeleteUserAddress;
using Marketplace.Application.Shipping.Commands.SetDefaultUserAddress;
using Marketplace.Application.Shipping.Commands.UpdateUserAddress;
using Marketplace.Application.Shipping.Queries.ListMyAddresses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Shipping")]
[Route("me/addresses")]
[Authorize]
public sealed class AddressesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ShippingOptions _shippingOptions;

    public AddressesController(ISender sender, IOptions<ShippingOptions> shippingOptions)
    {
        _sender = sender;
        _shippingOptions = shippingOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "list_addresses"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ListMyAddressesQuery(actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertAddressRequest request, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "create_address"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(
            new CreateUserAddressCommand(
                actorId,
                request.Type,
                request.IsDefault,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Street,
                request.City,
                request.State,
                request.PostalCode,
                request.Country),
            ct);
        return result.ToActionResult();
    }

    [HttpPatch("{addressId:long}")]
    public async Task<IActionResult> Update(long addressId, [FromBody] UpsertAddressRequest request, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "update_address"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(
            new UpdateUserAddressCommand(
                actorId,
                addressId,
                request.Type,
                request.IsDefault,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Street,
                request.City,
                request.State,
                request.PostalCode,
                request.Country),
            ct);
        return result.ToActionResult();
    }

    [HttpDelete("{addressId:long}")]
    public async Task<IActionResult> Delete(long addressId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "delete_address"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new DeleteUserAddressCommand(actorId, addressId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{addressId:long}/default")]
    public async Task<IActionResult> SetDefault(long addressId, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "set_default_address"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new SetDefaultUserAddressCommand(actorId, addressId), ct);
        return result.ToActionResult();
    }
}

public sealed record UpsertAddressRequest(
    string Type,
    bool IsDefault,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
