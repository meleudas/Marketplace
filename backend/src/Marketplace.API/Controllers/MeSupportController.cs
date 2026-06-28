using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Queries.GetMySupportTickets;
using Marketplace.Application.Support.Queries.GetSupportTicketById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Support")]
[Route("me/support/tickets")]
[Authorize(Roles = "User,Buyer")]
public sealed class MeSupportController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SupportOptions _options;

    public MeSupportController(ISender sender, IOptions<SupportOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new GetMySupportTicketsQuery(actorId.ToString(), page, size), ct);
        if (result.IsSuccess)
            MarketplaceMetrics.SupportTicketsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "list_tickets")]);
        return result.ToActionResult();
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new GetSupportTicketByIdQuery(actorId.ToString(), id, IsPlatformStaff()),
            ct);
        if (result.IsSuccess)
            MarketplaceMetrics.SupportTicketsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "get_ticket")]);
        return result.ToActionResult();
    }

    private bool IsPlatformStaff() =>
        User.IsInRole("Moderator") || User.IsInRole("Admin") || User.IsInRole("Support");
}
