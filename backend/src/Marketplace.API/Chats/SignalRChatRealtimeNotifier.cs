using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Ports;
using Marketplace.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Chats;

public sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ChatsOptions _options;

    public SignalRChatRealtimeNotifier(IHubContext<ChatHub> hubContext, IOptions<ChatsOptions> options)
    {
        _hubContext = hubContext;
        _options = options.Value;
    }

    public Task NotifyMessageReceivedAsync(Guid chatId, long messageId, Guid senderId, string text, CancellationToken ct = default)
    {
        if (!_options.RealtimeEnabled)
            return Task.CompletedTask;

        return _hubContext.Clients.Group(ChatHub.GroupName(chatId))
            .SendAsync("MessageReceived", new { chatId, messageId, senderId, text }, ct);
    }

    public Task NotifyMessageReadAsync(Guid chatId, Guid userId, long messageId, CancellationToken ct = default)
    {
        if (!_options.RealtimeEnabled)
            return Task.CompletedTask;

        return _hubContext.Clients.Group(ChatHub.GroupName(chatId))
            .SendAsync("MessageRead", new { chatId, userId, messageId }, ct);
    }

    public Task NotifyChatArchivedAsync(Guid chatId, CancellationToken ct = default)
    {
        if (!_options.RealtimeEnabled)
            return Task.CompletedTask;

        return _hubContext.Clients.Group(ChatHub.GroupName(chatId))
            .SendAsync("ChatArchived", new { chatId }, ct);
    }
}
