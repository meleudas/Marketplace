using Marketplace.Application.Chats.Options;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Policies;

public sealed class ChatAntiSpamPolicy
{
    private readonly IMessageRepository _messages;
    private readonly ChatsOptions _options;

    public ChatAntiSpamPolicy(IMessageRepository messages, IOptions<ChatsOptions> options)
    {
        _messages = messages;
        _options = options.Value;
    }

    public async Task<(bool Allowed, string? Reason)> EvaluateAsync(
        ChatId chatId,
        Guid senderId,
        string text,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);
        var recentCount = await _messages.CountRecentBySenderAsync(chatId, senderId, windowStart, ct);
        if (recentCount >= Math.Max(1, _options.MessagesPerMinute))
            return (false, "rate exceeded: chat message rate limit");

        var duplicateWindow = now.AddSeconds(-Math.Max(1, _options.DuplicateWindowSeconds));
        var duplicate = await _messages.FindRecentDuplicateAsync(chatId, senderId, text.Trim(), duplicateWindow, ct);
        if (duplicate is not null)
            return (false, "rate exceeded: duplicate chat message");

        return (true, null);
    }
}
