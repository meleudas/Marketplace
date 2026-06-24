using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Returns.Commands.ProcessReturnRefund;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminReturns")]
[Route("admin/returns")]
[Authorize(Roles = "Admin")]
public sealed class AdminReturnsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;

    public AdminReturnsController(ISender sender, IHttpIdempotencyStore idempotency)
    {
        _sender = sender;
        _idempotency = idempotency;
    }

    [HttpPost("{returnId:long}/refund")]
    public async Task<IActionResult> Refund(long returnId, [FromBody] ProcessReturnRefundBody? body, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var adminUserId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var scope = $"return-refund:{returnId}:{adminUserId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(returnId.ToString(), adminUserId.ToString("N"));
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "admin_return_refund"));
        var result = await _sender.Send(new ProcessReturnRefundCommand(returnId, adminUserId, body?.Amount), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }
}

public sealed record ProcessReturnRefundBody(decimal? Amount);
