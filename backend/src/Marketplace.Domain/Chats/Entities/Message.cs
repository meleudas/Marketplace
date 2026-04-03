using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

public sealed class Message : AuditableSoftDeleteAggregateRoot<MessageId>
{
    private Message() { }

    public ChatId ChatId { get; private set; } = null!;
    public Guid SenderId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public JsonBlob Attachments { get; private set; } = JsonBlob.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public JsonBlob DeletedBy { get; private set; } = JsonBlob.Empty;
    public MessageId? ReplyToMessageId { get; private set; }
    public string? RawPayload { get; private set; }

    public static Message Reconstitute(
        MessageId id,
        ChatId chatId,
        Guid senderId,
        string text,
        JsonBlob attachments,
        bool isRead,
        DateTime? readAt,
        JsonBlob deletedBy,
        MessageId? replyToMessageId,
        string? rawPayload,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ChatId = chatId,
            SenderId = senderId,
            Text = text,
            Attachments = attachments,
            IsRead = isRead,
            ReadAt = readAt,
            DeletedBy = deletedBy,
            ReplyToMessageId = replyToMessageId,
            RawPayload = rawPayload,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
