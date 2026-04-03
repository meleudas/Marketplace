using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

public sealed class Chat : AuditableSoftDeleteAggregateRoot<ChatId>
{
    private Chat() { }

    public ChatType Type { get; private set; }
    public OrderId? OrderId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public string? LastMessageText { get; private set; }
    public Guid? LastMessageSenderId { get; private set; }
    public DateTime? LastMessageCreatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public JsonBlob Meta { get; private set; } = JsonBlob.Empty;
    public JsonBlob? ParticipantsSnapshot { get; private set; }
    public string? RawPayload { get; private set; }

    public static Chat Reconstitute(
        ChatId id,
        ChatType type,
        OrderId? orderId,
        ProductId? productId,
        string? lastMessageText,
        Guid? lastMessageSenderId,
        DateTime? lastMessageCreatedAt,
        bool isActive,
        JsonBlob meta,
        JsonBlob? participantsSnapshot,
        string? rawPayload,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            Type = type,
            OrderId = orderId,
            ProductId = productId,
            LastMessageText = lastMessageText,
            LastMessageSenderId = lastMessageSenderId,
            LastMessageCreatedAt = lastMessageCreatedAt,
            IsActive = isActive,
            Meta = meta,
            ParticipantsSnapshot = participantsSnapshot,
            RawPayload = rawPayload,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
