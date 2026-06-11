using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

public sealed class Chat : AuditableSoftDeleteAggregateRoot<ChatId>
{
    private Chat() { }

    public ChatType Type { get; private set; }
    public ChatStatus Status { get; private set; }
    public Guid InitiatorUserId { get; private set; }
    public OrderId? OrderId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public string? LastMessageText { get; private set; }
    public Guid? LastMessageSenderId { get; private set; }
    public DateTime? LastMessageCreatedAt { get; private set; }
    public JsonBlob Meta { get; private set; } = JsonBlob.Empty;
    public JsonBlob? ParticipantsSnapshot { get; private set; }
    public string? RawPayload { get; private set; }

    public bool IsActive => Status == ChatStatus.Active;

    public static Chat CreateDirect(
        Guid buyerId,
        ProductId productId,
        DateTime nowUtc) =>
        Create(ChatType.Direct, buyerId, productId: productId, orderId: null, nowUtc);

    public static Chat CreateOrderRelated(
        Guid buyerId,
        OrderId orderId,
        DateTime nowUtc) =>
        Create(ChatType.OrderRelated, buyerId, productId: null, orderId: orderId, nowUtc);

    public static Chat CreateSupport(Guid buyerId, DateTime nowUtc) =>
        Create(ChatType.Support, buyerId, productId: null, orderId: null, nowUtc);

    private static Chat Create(
        ChatType type,
        Guid initiatorUserId,
        ProductId? productId,
        OrderId? orderId,
        DateTime nowUtc) =>
        new()
        {
            Id = ChatId.From(Guid.NewGuid()),
            Type = type,
            Status = ChatStatus.Active,
            InitiatorUserId = initiatorUserId,
            OrderId = orderId,
            ProductId = productId,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            IsDeleted = false,
            DeletedAt = null
        };

    public bool CanAcceptMessage() => Status == ChatStatus.Active;

    public void Archive(Guid actorId, DateTime nowUtc)
    {
        if (Status == ChatStatus.Archived)
            return;
        if (Status == ChatStatus.Blocked)
            throw new InvalidOperationException("Cannot archive blocked chat.");

        Status = ChatStatus.Archived;
        UpdatedAt = nowUtc;
        _ = actorId;
    }

    public void Block(string reason, DateTime nowUtc)
    {
        Status = ChatStatus.Blocked;
        Meta = new JsonBlob(System.Text.Json.JsonSerializer.Serialize(new { blockReason = reason }));
        UpdatedAt = nowUtc;
    }

    public void RecordLastMessage(string text, Guid senderId, DateTime atUtc)
    {
        LastMessageText = text.Length > 500 ? text[..500] : text;
        LastMessageSenderId = senderId;
        LastMessageCreatedAt = atUtc;
        UpdatedAt = atUtc;
    }

    public static Chat Reconstitute(
        ChatId id,
        ChatType type,
        ChatStatus status,
        Guid initiatorUserId,
        OrderId? orderId,
        ProductId? productId,
        string? lastMessageText,
        Guid? lastMessageSenderId,
        DateTime? lastMessageCreatedAt,
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
            Status = status,
            InitiatorUserId = initiatorUserId,
            OrderId = orderId,
            ProductId = productId,
            LastMessageText = lastMessageText,
            LastMessageSenderId = lastMessageSenderId,
            LastMessageCreatedAt = lastMessageCreatedAt,
            Meta = meta,
            ParticipantsSnapshot = participantsSnapshot,
            RawPayload = rawPayload,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
