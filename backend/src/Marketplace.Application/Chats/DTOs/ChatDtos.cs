namespace Marketplace.Application.Chats.DTOs;

public sealed record ChatDto(
    Guid Id,
    short Type,
    short Status,
    long? OrderId,
    long? ProductId,
    string? LastMessageText,
    Guid? LastMessageSenderId,
    DateTime? LastMessageCreatedAt,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ChatMessageDto(
    long Id,
    Guid ChatId,
    Guid SenderId,
    string Text,
    short Status,
    DateTime? ReadAt,
    long? ReplyToMessageId,
    DateTime CreatedAt);

public sealed record ChatListDto(
    IReadOnlyList<ChatDto> Items,
    int Total,
    int Page,
    int Size);

public sealed record ChatMessagesDto(
    IReadOnlyList<ChatMessageDto> Items,
    int Total,
    int Page,
    int Size);

public sealed record ChatModerationResultDto(
    long ActionId,
    Guid ChatId,
    long? MessageId,
    short ActionType,
    short ChatStatus);
