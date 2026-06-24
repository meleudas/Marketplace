using Marketplace.API.Extensions;
using Marketplace.Application.Chats.Commands.ArchiveChat;
using Marketplace.Application.Chats.Commands.CreateChat;
using Marketplace.Application.Chats.Commands.MarkMessageRead;
using Marketplace.Application.Chats.Commands.SendMessage;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Queries.GetChatMessages;
using Marketplace.Application.Chats.Queries.ListMyChats;
using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Chats")]
[Route("me/chats")]
[Authorize(Roles = "User,Buyer,Seller")]
public sealed class ChatsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ChatsOptions _options;

    public ChatsController(ISender sender, IOptions<ChatsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChatRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ChatMessageLatencyMs, new KeyValuePair<string, object?>("operation", "create_chat"));
        var result = await _sender.Send(
            new CreateChatCommand(actorId, request.Type, request.ProductId, request.OrderId),
            ct);
        Track("create_chat", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new ListMyChatsQuery(actorId, page, size), ct);
        TrackList(result);
        return result.ToActionResult();
    }

    [HttpGet("{chatId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid chatId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new GetChatMessagesQuery(actorId, chatId, page, size, IsPlatformStaff()),
            ct);
        Track("get_messages", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{chatId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid chatId, [FromBody] SendChatMessageRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ChatMessageLatencyMs, new KeyValuePair<string, object?>("operation", "send_message"));
        var result = await _sender.Send(
            new SendMessageCommand(actorId, chatId, request.Text, request.ReplyToMessageId, IsPlatformStaff()),
            ct);
        Track("send_message", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{chatId:guid}/messages/{messageId:long}/read")]
    public async Task<IActionResult> MarkRead(Guid chatId, long messageId, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new MarkMessageReadCommand(actorId, chatId, messageId, IsPlatformStaff()),
            ct);
        Track("mark_read", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{chatId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid chatId, CancellationToken ct)
    {
        if (!_options.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(
            new ArchiveChatCommand(actorId, chatId, IsPlatformStaff()),
            ct);
        Track("archive_chat", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    private bool IsPlatformStaff() =>
        User.IsInRole("Moderator") || User.IsInRole("Admin") || User.IsInRole("Support");

    private static void TrackList(Result<ChatListDto> result)
    {
        if (result is { IsSuccess: true, Value: not null })
        {
            MarketplaceMetrics.ChatMessagesTotal.Add(1, [new KeyValuePair<string, object?>("operation", "list_chats"), new KeyValuePair<string, object?>("status", "success")]);
            var backlog = result.Value.Items.Sum(x => x.UnreadCount);
            MarketplaceMetrics.ChatUnreadBacklog.Add(backlog);
            return;
        }

        MarketplaceMetrics.ChatMessageErrorsTotal.Add(1, [new KeyValuePair<string, object?>("operation", "list_chats")]);
    }

    private static void Track(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.ChatMessagesTotal.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.ChatMessageErrorsTotal.Add(1, [new KeyValuePair<string, object?>("operation", operation)]);
        if ((error ?? string.Empty).Contains("rate exceeded", StringComparison.OrdinalIgnoreCase))
            MarketplaceMetrics.ChatSpamBlockTotal.Add(1);
    }
}

public sealed record CreateChatRequest(short Type, long? ProductId, long? OrderId);
public sealed record SendChatMessageRequest(string Text, long? ReplyToMessageId);
