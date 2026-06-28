using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Commands.CalculateShippingQuote;
using Marketplace.Application.Shipping.Queries.GetShippingMethods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Shipping")]
[Route("shipping")]
public sealed class ShippingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ShippingOptions _shippingOptions;

    public ShippingController(ISender sender, IOptions<ShippingOptions> shippingOptions)
    {
        _sender = sender;
        _shippingOptions = shippingOptions.Value;
    }

    [HttpGet("methods")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMethods(CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "get_methods"));
        var result = await _sender.Send(new GetShippingMethodsQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("quote")]
    [Authorize]
    public async Task<IActionResult> CalculateQuote([FromBody] CalculateShippingQuoteRequest request, CancellationToken ct)
    {
        if (!_shippingOptions.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ShippingLatencyMs, new KeyValuePair<string, object?>("operation", "calculate_quote"));
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new CalculateShippingQuoteCommand(
                actorId,
                request.ShippingMethodId,
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
}

public sealed record CalculateShippingQuoteRequest(
    long ShippingMethodId,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
