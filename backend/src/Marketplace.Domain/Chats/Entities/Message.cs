using Marketplace.Domain.Chats.Enums;
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
    public MessageStatus Status { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public JsonBlob DeletedBy { get; private set; } = JsonBlob.Empty;
    public MessageId? ReplyToMessageId { get; private set; }
    public string? RawPayload { get; private set; }

    public bool IsRead => Status == MessageStatus.Read;

    public static Message Send(
        ChatId chatId,
        Guid senderId,
        string text,
        JsonBlob attachments,
        MessageId? replyToMessageId,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Message text is required.", nameof(text));

        return new Message
        {
            Id = MessageId.From(0),
            ChatId = chatId,
            SenderId = senderId,
            Text = text.Trim(),
            Attachments = attachments,
            Status = MessageStatus.Sent,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public void MarkRead(DateTime nowUtc)
    {
        if (Status == MessageStatus.DeletedForPolicy)
            return;

        Status = MessageStatus.Read;
        ReadAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public void MarkDeletedForPolicy(Guid moderatorId, string reason, DateTime nowUtc)
    {
        Status = MessageStatus.DeletedForPolicy;
        DeletedBy = new JsonBlob(System.Text.Json.JsonSerializer.Serialize(new
        {
            moderatorId,
            reason,
            at = nowUtc
        }));
        Text = "[removed by moderation]";
        UpdatedAt = nowUtc;
    }

    public static Message Reconstitute(
        MessageId id,
        ChatId chatId,
        Guid senderId,
        string text,
        JsonBlob attachments,
        MessageStatus status,
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
            Status = status,
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
