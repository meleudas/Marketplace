using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

public sealed class ChatModerationAction : Entity
{
    private ChatModerationAction() { }

    public long Id { get; private set; }
    public ChatId ChatId { get; private set; } = null!;
    public MessageId? MessageId { get; private set; }
    public ChatModerationActionType ActionType { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static ChatModerationAction Create(
        ChatId chatId,
        MessageId? messageId,
        ChatModerationActionType actionType,
        Guid moderatorUserId,
        string reason,
        DateTime nowUtc) =>
        new()
        {
            ChatId = chatId,
            MessageId = messageId,
            ActionType = actionType,
            ModeratorUserId = moderatorUserId,
            Reason = reason.Trim(),
            CreatedAt = nowUtc
        };

    public static ChatModerationAction Reconstitute(
        long id,
        ChatId chatId,
        MessageId? messageId,
        ChatModerationActionType actionType,
        Guid moderatorUserId,
        string reason,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            ChatId = chatId,
            MessageId = messageId,
            ActionType = actionType,
            ModeratorUserId = moderatorUserId,
            Reason = reason,
            CreatedAt = createdAt
        };
}
