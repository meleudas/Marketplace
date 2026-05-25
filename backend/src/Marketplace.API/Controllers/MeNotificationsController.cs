using System.Text.Json;
using Marketplace.API.Extensions;
using Marketplace.Application.Notifications.Commands.MarkNotificationRead;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("me/in-app-notifications")]
[Authorize]
public sealed class MeNotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public MeNotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _sender.Send(new GetMyNotificationsQuery(userId, page, pageSize), ct);
        if (result.IsFailure)
            return result.ToActionResult();

        var p = result.Value!;
        var items = p.Items.Select(MapItem).ToList();
        return Ok(new { items, total = p.Total, page = p.Page, pageSize = p.PageSize });
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _sender.Send(new MarkNotificationReadCommand(userId, id), ct);
        return result.ToActionResult();
    }

    private static object MapItem(InAppNotificationListItemDto i)
    {
        object? data = null;
        try
        {
            data = JsonSerializer.Deserialize<object>(i.DataJson);
        }
        catch
        {
            data = i.DataJson;
        }

        return new
        {
            i.Id,
            i.TemplateKey,
            i.CorrelationId,
            kind = i.Kind.ToString(),
            i.Title,
            message = i.Message,
            i.ActionUrl,
            i.IsRead,
            i.ReadAt,
            createdAt = i.CreatedAtUtc,
            data
        };
    }
}
