using Marketplace.API.Extensions;
using Marketplace.Application.Payments.Commands.RequestRefund;
using Marketplace.Application.Payments.Commands.SyncPaymentStatus;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/payments")]
[Authorize(Roles = "Admin")]
public sealed class AdminPaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminPaymentsController(ISender sender) => _sender = sender;

    [HttpPost("{paymentId:long}/refund")]
    public async Task<IActionResult> Refund(long paymentId, [FromBody] RequestRefundBody body, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "admin_payments_refund"));
        if (!User.TryGetUserId(out var adminUserId))
        {
            MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", "admin_payments_refund"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new RequestRefundCommand(paymentId, body.Amount, body.Reason, adminUserId), ct);
        TrackPaymentResult("admin_payments_refund", result);
        return result.ToActionResult();
    }

    [HttpPost("{paymentId:long}/sync")]
    public async Task<IActionResult> Sync(long paymentId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "admin_payments_sync"));
        if (!User.TryGetUserId(out _))
        {
            MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", "admin_payments_sync"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new SyncPaymentStatusCommand(paymentId), ct);
        TrackPaymentResult("admin_payments_sync", result);
        return result.ToActionResult();
    }

    private static void TrackPaymentResult(string operation, Domain.Shared.Kernel.Result result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.PaymentOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        var reason = string.Equals(result.Error, "Payment not found", StringComparison.OrdinalIgnoreCase)
            ? "not_found"
            : string.Equals(result.Error, "Forbidden", StringComparison.OrdinalIgnoreCase)
                ? "forbidden"
                : "application_failure";
        MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", reason)]);
    }
}

public sealed record RequestRefundBody(decimal Amount, string Reason);
