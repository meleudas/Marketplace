using Marketplace.API.Extensions;
using Marketplace.Application.Payments.Commands.RequestRefund;
using Marketplace.Application.Payments.Commands.SyncPaymentStatus;
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
        if (!User.TryGetUserId(out var adminUserId))
            return Unauthorized();

        var result = await _sender.Send(new RequestRefundCommand(paymentId, body.Amount, body.Reason, adminUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{paymentId:long}/sync")]
    public async Task<IActionResult> Sync(long paymentId, CancellationToken ct)
    {
        var result = await _sender.Send(new SyncPaymentStatusCommand(paymentId), ct);
        return result.ToActionResult();
    }
}

public sealed record RequestRefundBody(decimal Amount, string Reason);
