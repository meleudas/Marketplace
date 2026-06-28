using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Returns.Commands.ApproveReturn;
using Marketplace.Application.Returns.Commands.MarkReturnReceived;
using Marketplace.Application.Returns.Commands.RejectReturn;
using Marketplace.Application.Returns.Queries.GetReturnById;
using Marketplace.Application.Returns.Queries.ListCompanyReturns;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("CompanyReturns")]
[Authorize]
public sealed class CompanyReturnsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;

    public CompanyReturnsController(ISender sender, IHttpIdempotencyStore idempotency)
    {
        _sender = sender;
        _idempotency = idempotency;
    }

    [HttpGet("companies/{companyId:guid}/returns")]
    public async Task<IActionResult> List(Guid companyId, [FromQuery] string? status, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ListCompanyReturnsQuery(companyId, actorId, User.IsInRole("Admin"), status), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/returns/{returnId:long}")]
    public async Task<IActionResult> Get(Guid companyId, long returnId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetReturnByIdQuery(returnId, actorId, User.IsInRole("Admin"), true, companyId), ct);
        return result.ToActionResult();
    }

    [HttpPost("companies/{companyId:guid}/returns/{returnId:long}/approve")]
    public async Task<IActionResult> Approve(Guid companyId, long returnId, CancellationToken ct)
    {
        return await ExecuteWriteAsync(companyId, returnId, "approve", async actorId =>
            await _sender.Send(new ApproveReturnCommand(returnId, companyId, actorId, User.IsInRole("Admin")), ct), ct);
    }

    [HttpPost("companies/{companyId:guid}/returns/{returnId:long}/reject")]
    public async Task<IActionResult> Reject(Guid companyId, long returnId, [FromBody] RejectReturnBody body, CancellationToken ct)
    {
        return await ExecuteWriteAsync(companyId, returnId, "reject", async actorId =>
            await _sender.Send(new RejectReturnCommand(returnId, companyId, actorId, User.IsInRole("Admin"), body.Reason), ct), ct);
    }

    [HttpPost("companies/{companyId:guid}/returns/{returnId:long}/received")]
    public async Task<IActionResult> MarkReceived(Guid companyId, long returnId, CancellationToken ct)
    {
        return await ExecuteWriteAsync(companyId, returnId, "received", async actorId =>
            await _sender.Send(new MarkReturnReceivedCommand(returnId, companyId, actorId, User.IsInRole("Admin")), ct), ct);
    }

    private async Task<IActionResult> ExecuteWriteAsync<T>(
        Guid companyId,
        long returnId,
        string action,
        Func<Guid, Task<Domain.Shared.Kernel.Result<T>>> send,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var scope = $"return-{action}:{companyId:N}:{returnId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(companyId.ToString("N"), returnId.ToString(), action);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", $"returns_{action}"));
        var result = await send(actorId);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }
}

public sealed record RejectReturnBody(string Reason);
