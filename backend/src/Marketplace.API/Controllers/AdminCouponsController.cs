using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Coupons.Commands.CreateCoupon;
using Marketplace.Application.Coupons.Commands.DeactivateCoupon;
using Marketplace.Application.Coupons.Commands.UpdateCoupon;
using Marketplace.Application.Coupons.Options;
using Marketplace.Application.Coupons.Queries.GetCouponUsageReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/coupons")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminCouponsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly CouponsOptions _options;

    public AdminCouponsController(ISender sender, IOptions<CouponsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CheckoutLatencyMs, new KeyValuePair<string, object?>("operation", "admin_coupon_create"));
        var result = await _sender.Send(
            new CreateCouponCommand(
                request.Code,
                request.Description,
                request.DiscountAmount,
                request.DiscountType,
                request.MinOrderAmount,
                request.UsageLimit,
                request.UserUsageLimit,
                request.StartsAtUtc,
                request.ExpiresAtUtc,
                request.IsActive,
                request.ApplicableCompaniesJson),
            ct);
        Track("admin_coupon_create", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCouponRequest request, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CheckoutLatencyMs, new KeyValuePair<string, object?>("operation", "admin_coupon_update"));
        var result = await _sender.Send(
            new UpdateCouponCommand(
                id,
                request.Description,
                request.DiscountAmount,
                request.DiscountType,
                request.MinOrderAmount,
                request.UsageLimit,
                request.UserUsageLimit,
                request.StartsAtUtc,
                request.ExpiresAtUtc,
                request.IsActive,
                request.ApplicableCompaniesJson),
            ct);
        Track("admin_coupon_update", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var result = await _sender.Send(new DeactivateCouponCommand(id), ct);
        Track("admin_coupon_deactivate", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("{id:long}/usage")]
    public async Task<IActionResult> GetUsage(long id, CancellationToken ct)
    {
        if (!_options.ReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var result = await _sender.Send(new GetCouponUsageReportQuery(id), ct);
        Track("admin_coupon_usage", result.IsSuccess, result.Error);
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

public sealed record CreateCouponRequest(
    string Code,
    string? Description,
    decimal DiscountAmount,
    string DiscountType,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UserUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    string? ApplicableCompaniesJson);

public sealed record UpdateCouponRequest(
    string? Description,
    decimal DiscountAmount,
    string DiscountType,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UserUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    string? ApplicableCompaniesJson);
