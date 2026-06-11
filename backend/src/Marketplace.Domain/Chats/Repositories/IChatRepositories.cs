using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Repositories;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct = default);
    Task<Chat?> FindActiveDirectAsync(ProductId productId, Guid buyerId, CancellationToken ct = default);
    Task<Chat?> FindActiveOrderRelatedAsync(OrderId orderId, Guid buyerId, CancellationToken ct = default);
    Task<Chat?> FindActiveSupportAsync(Guid buyerId, CancellationToken ct = default);
    Task<IReadOnlyList<Chat>> ListForParticipantAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> CountForParticipantAsync(Guid userId, CancellationToken ct = default);
    Task<Chat> AddAsync(Chat chat, CancellationToken ct = default);
    Task UpdateAsync(Chat chat, CancellationToken ct = default);
}

public interface IChatParticipantRepository
{
    Task<ChatParticipant?> GetAsync(ChatId chatId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatParticipant>> ListByChatAsync(ChatId chatId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatParticipant>> ListActiveByChatAsync(ChatId chatId, CancellationToken ct = default);
    Task<bool> IsActiveParticipantAsync(ChatId chatId, Guid userId, CancellationToken ct = default);
    Task AddAsync(ChatParticipant participant, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<ChatParticipant> participants, CancellationToken ct = default);
}

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> ListByChatAsync(ChatId chatId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByChatAsync(ChatId chatId, CancellationToken ct = default);
    Task<int> CountRecentBySenderAsync(ChatId chatId, Guid senderId, DateTime sinceUtc, CancellationToken ct = default);
    Task<Message?> FindRecentDuplicateAsync(ChatId chatId, Guid senderId, string normalizedText, DateTime sinceUtc, CancellationToken ct = default);
    Task<int> CountUnreadForUserAsync(ChatId chatId, Guid userId, MessageId? afterMessageId, CancellationToken ct = default);
    Task<Message> AddAsync(Message message, CancellationToken ct = default);
    Task UpdateAsync(Message message, CancellationToken ct = default);
}

public interface IChatReadStateRepository
{
    Task<ChatReadState?> GetAsync(ChatId chatId, Guid userId, CancellationToken ct = default);
    Task UpsertAsync(ChatReadState state, CancellationToken ct = default);
}

public interface IChatModerationActionRepository
{
    Task<ChatModerationAction> AppendAsync(ChatModerationAction action, CancellationToken ct = default);
    Task<IReadOnlyList<ChatModerationAction>> ListByChatAsync(ChatId chatId, int limit, CancellationToken ct = default);
}
