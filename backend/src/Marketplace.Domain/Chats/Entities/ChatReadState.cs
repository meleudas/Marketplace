using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

public sealed class ChatReadState : Entity
{
    private ChatReadState() { }

    public ChatId ChatId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public MessageId LastReadMessageId { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }

    public static ChatReadState Create(ChatId chatId, Guid userId, MessageId lastReadMessageId, DateTime nowUtc) =>
        new()
        {
            ChatId = chatId,
            UserId = userId,
            LastReadMessageId = lastReadMessageId,
            UpdatedAt = nowUtc
        };

    public static ChatReadState Reconstitute(
        ChatId chatId,
        Guid userId,
        MessageId lastReadMessageId,
        DateTime updatedAt) =>
        new()
        {
            ChatId = chatId,
            UserId = userId,
            LastReadMessageId = lastReadMessageId,
            UpdatedAt = updatedAt
        };

    public void AdvanceTo(MessageId messageId, DateTime nowUtc)
    {
        if (messageId.Value <= LastReadMessageId.Value)
            return;

        LastReadMessageId = messageId;
        UpdatedAt = nowUtc;
    }
}
