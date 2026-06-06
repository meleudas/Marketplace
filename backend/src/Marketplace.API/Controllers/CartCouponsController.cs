using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Coupons.Commands.ApplyCouponToCart;
using Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;
using Marketplace.Application.Coupons.Commands.ValidateCouponForCart;
using Marketplace.Application.Coupons.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("CartCoupons")]
[Route("me/cart/coupons")]
[Authorize]
public sealed class CartCouponsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly CouponsOptions _options;

    public CartCouponsController(ISender sender, IOptions<CouponsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] CouponCodeRequest request, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ValidateCouponForCartCommand(actorId, request.Code), ct);
        Track("coupon_validate", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] CouponCodeRequest request, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ApplyCouponToCartCommand(actorId, request.Code), ct);
        Track("coupon_apply", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Remove(string code, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new RemoveCouponFromCartCommand(actorId, code), ct);
        Track("coupon_remove", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    private static void Track(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.CouponOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.CouponErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        if ((error ?? string.Empty).Contains("unprocessable", StringComparison.OrdinalIgnoreCase))
            MarketplaceMetrics.CouponValidationFailures.Add(1, [new KeyValuePair<string, object?>("operation", operation)]);
    }
}

public sealed record CouponCodeRequest(string Code);
