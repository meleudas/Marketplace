using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Returns.Commands.RequestReturn;
using Marketplace.Application.Returns.Queries.GetReturnById;
using Marketplace.Application.Returns.Queries.ListMyReturns;
using Marketplace.Domain.Returns.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Returns")]
[Route("me")]
[Authorize]
public sealed class ReturnsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;

    public ReturnsController(ISender sender, IHttpIdempotencyStore idempotency)
    {
        _sender = sender;
        _idempotency = idempotency;
    }

    [HttpPost("orders/{orderId:long}/returns")]
    public async Task<IActionResult> RequestReturn(long orderId, [FromBody] RequestReturnBody body, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");
        if (!Enum.TryParse<ReturnReasonCode>(body.ReasonCode, true, out var reasonCode))
            return BadRequest("Invalid reason code");

        var scope = $"return-request:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(orderId.ToString(), actorId.ToString("N"), body.ReasonCode);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "returns_request"));
        var result = await _sender.Send(new RequestReturnCommand(
            orderId,
            actorId,
            reasonCode,
            body.Comment,
            body.Lines), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }

    [HttpGet("returns")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ListMyReturnsQuery(actorId), ct);
        return result.ToActionResult();
    }

    [HttpGet("returns/{returnId:long}")]
    public async Task<IActionResult> Get(long returnId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetReturnByIdQuery(returnId, actorId, User.IsInRole("Admin"), false, null), ct);
        return result.ToActionResult();
    }
}

public sealed record RequestReturnBody(string ReasonCode, string? Comment, IReadOnlyList<RequestReturnLineDto> Lines);
