using Marketplace.API.Extensions;
using Marketplace.Application.Chats.Commands.ModerateChatMessage;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Chats")]
[Route("admin/chats")]
[Authorize(Roles = "Moderator,Admin,Support")]
public sealed class AdminChatsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ChatsOptions _options;

    public AdminChatsController(ISender sender, IOptions<ChatsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("{chatId:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid chatId, [FromBody] ModerateChatRequest request, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.ModerationEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ChatMessageLatencyMs, new KeyValuePair<string, object?>("operation", "moderate_chat"));
        var result = await _sender.Send(
            new ModerateChatMessageCommand(actorId, chatId, request.MessageId, request.ActionType, request.Reason),
            ct);

        if (result.IsSuccess)
            MarketplaceMetrics.ChatMessagesTotal.Add(1, [new KeyValuePair<string, object?>("operation", "moderate_chat"), new KeyValuePair<string, object?>("status", "success")]);
        else
            MarketplaceMetrics.ChatMessageErrorsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "moderate_chat")]);

        return result.ToActionResult();
    }
}

public sealed record ModerateChatRequest(long? MessageId, short ActionType, string Reason);
