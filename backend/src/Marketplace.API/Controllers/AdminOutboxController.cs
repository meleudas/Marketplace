using Marketplace.Application.Common.DTOs;
using Marketplace.Application.Common.Ports;
using Marketplace.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminOutbox")]
[Route("admin/outbox")]
[Authorize(Roles = "Admin")]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly IOutboxWriter _outbox;

    public AdminOutboxController(IOutboxWriter outbox)
    {
        _outbox = outbox;
    }

    [HttpGet("dead-letters")]
    public async Task<ActionResult<PagedOutboxMessagesDto>> ListDeadLetters(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized();
        if (!User.IsInRole("Admin"))
            return Forbid();

        var (items, total) = await _outbox.ListDeadLettersAsync(page, pageSize, ct);
        return Ok(MapPage(items, total, page, pageSize));
    }

    [HttpGet("stuck")]
    public async Task<ActionResult<PagedOutboxMessagesDto>> ListStuck(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized();
        if (!User.IsInRole("Admin"))
            return Forbid();

        var (items, total) = await _outbox.ListStuckAsync(DateTime.UtcNow, page, pageSize, ct);
        return Ok(MapPage(items, total, page, pageSize));
    }

    [HttpPost("{messageId:guid}/requeue")]
    public async Task<IActionResult> Requeue(Guid messageId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized();
        if (!User.IsInRole("Admin"))
            return Forbid();

        await _outbox.RequeueDeadLetterAsync(messageId, ct);
        return Ok();
    }

    private static PagedOutboxMessagesDto MapPage(
        IReadOnlyList<OutboxMessage> items,
        long total,
        int page,
        int pageSize) =>
        new(
            items.Select(x => new OutboxMessageAdminDto(
                x.Id,
                x.AggregateType,
                x.EventType,
                x.Attempts,
                x.LastError,
                x.DeadLetterCategory,
                x.OccurredAtUtc)).ToList(),
            total,
            page,
            pageSize);
}
