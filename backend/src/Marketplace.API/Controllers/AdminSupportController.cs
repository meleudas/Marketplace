using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Support.Commands.AssignSupportTicket;
using Marketplace.Application.Support.Commands.EscalateSupportTicket;
using Marketplace.Application.Support.Commands.UpdateTicketStatus;
using Marketplace.Application.Support.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminSupport")]
[Route("admin/support/tickets")]
[Authorize(Roles = "Moderator,Admin,Support")]
public sealed class AdminSupportController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SupportOptions _options;

    public AdminSupportController(ISender sender, IOptions<SupportOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("{id:long}/assign")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignSupportTicketRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new AssignSupportTicketCommand(actorId.ToString(), id, request.AssigneeUserId, request.Reason),
            ct);
        Track("assign", result.IsSuccess);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/status")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateSupportTicketStatusRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.SupportTicketLatencyMs,
            new KeyValuePair<string, object?>("operation", "update_status"));
        var result = await _sender.Send(
            new UpdateTicketStatusCommand(actorId.ToString(), id, request.Status, request.Reason, IsStaff: true),
            ct);
        Track("update_status", result.IsSuccess);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/escalate")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Escalate(long id, [FromBody] EscalateSupportTicketRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new EscalateSupportTicketCommand(actorId.ToString(), id, request.Reason),
            ct);
        Track("escalate", result.IsSuccess);
        return result.ToActionResult();
    }

    private static void Track(string operation, bool success)
    {
        if (success)
            MarketplaceMetrics.SupportTicketsTotal.Add(1,
            [
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("status", "success")
            ]);
        else
            MarketplaceMetrics.SupportTicketErrorsTotal.Add(1, [new KeyValuePair<string, object?>("operation", operation)]);
    }
}

public sealed record AssignSupportTicketRequest(string AssigneeUserId, string Reason);
public sealed record UpdateSupportTicketStatusRequest(short Status, string Reason);
public sealed record EscalateSupportTicketRequest(string Reason);
