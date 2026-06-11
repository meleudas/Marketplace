using Marketplace.Application.Chats.DTOs;
using Marketplace.Domain.Chats.Entities;

namespace Marketplace.Application.Chats;

internal static class ChatsMappers
{
    public static ChatDto ToDto(this Chat entity, int unreadCount = 0) =>
        new(
            entity.Id.Value,
            (short)entity.Type,
            (short)entity.Status,
            entity.OrderId?.Value,
            entity.ProductId?.Value,
            entity.LastMessageText,
            entity.LastMessageSenderId,
            entity.LastMessageCreatedAt,
            unreadCount,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static ChatMessageDto ToDto(this Message entity) =>
        new(
            entity.Id.Value,
            entity.ChatId.Value,
            entity.SenderId,
            entity.Text,
            (short)entity.Status,
            entity.ReadAt,
            entity.ReplyToMessageId?.Value,
            entity.CreatedAt);
}
