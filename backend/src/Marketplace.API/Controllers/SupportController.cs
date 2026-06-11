using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Support.Commands.AddSupportMessage;
using Marketplace.Application.Support.Commands.CreateSupportTicket;
using Marketplace.Application.Support.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Support")]
[Route("support/tickets")]
[Authorize(Roles = "User,Buyer")]
public sealed class SupportController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SupportOptions _options;

    public SupportController(ISender sender, IOptions<SupportOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.SupportTicketLatencyMs,
            new KeyValuePair<string, object?>("operation", "create_ticket"));
        var result = await _sender.Send(
            new CreateSupportTicketCommand(
                actorId.ToString(),
                request.Subject,
                request.Message,
                request.Priority,
                request.OrderId,
                request.CompanyId,
                request.CategoryId),
            ct);
        Track("create_ticket", result.IsSuccess);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/messages")]
    public async Task<IActionResult> AddMessage(long id, [FromBody] AddSupportMessageRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.SupportTicketLatencyMs,
            new KeyValuePair<string, object?>("operation", "add_message"));
        var result = await _sender.Send(
            new AddSupportMessageCommand(
                actorId.ToString(),
                id,
                request.Message,
                request.IsInternal,
                IsPlatformStaff()),
            ct);
        Track("add_message", result.IsSuccess);
        return result.ToActionResult();
    }

    private bool IsPlatformStaff() =>
        User.IsInRole("Moderator") || User.IsInRole("Admin") || User.IsInRole("Support");

    private static void Track(string operation, bool success)
    {
        if (success)
        {
            MarketplaceMetrics.SupportTicketsTotal.Add(1,
            [
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("status", "success")
            ]);
            return;
        }

        MarketplaceMetrics.SupportTicketErrorsTotal.Add(1,
            [new KeyValuePair<string, object?>("operation", operation)]);
    }
}

public sealed record CreateSupportTicketRequest(
    string Subject,
    string Message,
    short Priority,
    long? OrderId,
    Guid? CompanyId,
    long? CategoryId);

public sealed record AddSupportMessageRequest(string Message, bool IsInternal = false);
