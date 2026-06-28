using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Chats")]
public sealed class ApplicationChatsDomainTests
{
    [Fact]
    public void Chat_Archived_Cannot_Accept_Messages()
    {
        var buyer = Guid.NewGuid();
        var chat = Chat.CreateSupport(buyer, DateTime.UtcNow);
        chat.Archive(buyer, DateTime.UtcNow);

        Assert.False(chat.CanAcceptMessage());
        Assert.Equal(ChatStatus.Archived, chat.Status);
    }

    [Fact]
    public void ChatReadState_AdvanceTo_Is_Idempotent()
    {
        var chatId = ChatId.From(Guid.NewGuid());
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var state = ChatReadState.Create(chatId, userId, MessageId.From(1), now);

        state.AdvanceTo(MessageId.From(5), now.AddMinutes(1));
        state.AdvanceTo(MessageId.From(3), now.AddMinutes(2));

        Assert.Equal(5, state.LastReadMessageId.Value);
    }

    [Fact]
    public void Message_MarkDeletedForPolicy_Redacts_Text()
    {
        var message = Message.Send(
            ChatId.From(Guid.NewGuid()),
            Guid.NewGuid(),
            "bad content",
            JsonBlob.Empty,
            null,
            DateTime.UtcNow);

        message.MarkDeletedForPolicy(Guid.NewGuid(), "policy", DateTime.UtcNow);

        Assert.Equal(MessageStatus.DeletedForPolicy, message.Status);
        Assert.Contains("removed", message.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentModerationPolicy_Blocks_Prohibited_Pattern()
    {
        var policy = new ChatContentModerationPolicy(Options.Create(new ChatsOptions
        {
            ModerationEnabled = true,
            ProhibitedPatterns = ["(?i)spam"]
        }));

        var result = policy.Evaluate("this is SPAM text");
        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task AntiSpamPolicy_Blocks_Duplicate_Message()
    {
        var chatId = ChatId.From(Guid.NewGuid());
        var senderId = Guid.NewGuid();
        var repo = new InMemoryMessageRepository();
        var now = DateTime.UtcNow;
        await repo.AddAsync(Message.Send(chatId, senderId, "hello", JsonBlob.Empty, null, now), CancellationToken.None);

        var policy = new ChatAntiSpamPolicy(repo, Options.Create(new ChatsOptions
        {
            MessagesPerMinute = 10,
            DuplicateWindowSeconds = 60
        }));

        var result = await policy.EvaluateAsync(chatId, senderId, "hello", CancellationToken.None);
        Assert.False(result.Allowed);
        Assert.Contains("rate exceeded", result.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AccessPolicy_Allows_Support_Staff_For_Support_Chat()
    {
        var buyer = Guid.NewGuid();
        var chat = Chat.CreateSupport(buyer, DateTime.UtcNow);
        var chatRepo = new InMemoryChatRepository(chat);
        var participantRepo = new InMemoryParticipantRepository(
            ChatParticipant.Join(chat.Id, buyer, ChatParticipantRole.Buyer, null, DateTime.UtcNow));

        var policy = new ChatAccessPolicy(participantRepo, chatRepo);
        var staff = Guid.NewGuid();

        Assert.True(await policy.CanAccessAsync(chat.Id, buyer, isPlatformStaff: false));
        Assert.True(await policy.CanAccessAsync(chat.Id, staff, isPlatformStaff: true));
        Assert.False(await policy.CanAccessAsync(chat.Id, staff, isPlatformStaff: false));
    }

    private sealed class InMemoryChatRepository : IChatRepository
    {
        private readonly Chat _chat;

        public InMemoryChatRepository(Chat chat) => _chat = chat;

        public Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct = default) =>
            Task.FromResult<Chat?>(_chat.Id.Value == id.Value ? _chat : null);

        public Task<Chat?> FindActiveDirectAsync(ProductId productId, Guid buyerId, CancellationToken ct = default) =>
            Task.FromResult<Chat?>(null);

        public Task<Chat?> FindActiveOrderRelatedAsync(OrderId orderId, Guid buyerId, CancellationToken ct = default) =>
            Task.FromResult<Chat?>(null);

        public Task<Chat?> FindActiveSupportAsync(Guid buyerId, CancellationToken ct = default) =>
            Task.FromResult<Chat?>(null);

        public Task<IReadOnlyList<Chat>> ListForParticipantAsync(Guid userId, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Chat>>([]);

        public Task<int> CountForParticipantAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);

        public Task<Chat> AddAsync(Chat entity, CancellationToken ct = default) => Task.FromResult(entity);

        public Task UpdateAsync(Chat entity, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryParticipantRepository : IChatParticipantRepository
    {
        private readonly List<ChatParticipant> _items;

        public InMemoryParticipantRepository(params ChatParticipant[] items) => _items = items.ToList();

        public Task<ChatParticipant?> GetAsync(ChatId chatId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.ChatId.Value == chatId.Value && x.UserId == userId));

        public Task<IReadOnlyList<ChatParticipant>> ListByChatAsync(ChatId chatId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatParticipant>>(_items.Where(x => x.ChatId.Value == chatId.Value).ToList());

        public Task<IReadOnlyList<ChatParticipant>> ListActiveByChatAsync(ChatId chatId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatParticipant>>(_items.Where(x => x.ChatId.Value == chatId.Value && x.IsActive).ToList());

        public Task<bool> IsActiveParticipantAsync(ChatId chatId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_items.Any(x => x.ChatId.Value == chatId.Value && x.UserId == userId && x.IsActive));

        public Task AddAsync(ChatParticipant participant, CancellationToken ct = default)
        {
            _items.Add(participant);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IReadOnlyList<ChatParticipant> participants, CancellationToken ct = default)
        {
            _items.AddRange(participants);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMessageRepository : IMessageRepository
    {
        private readonly List<Message> _items = [];
        private long _nextId = 1;

        public Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<IReadOnlyList<Message>> ListByChatAsync(ChatId chatId, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Message>>(_items.Where(x => x.ChatId.Value == chatId.Value).ToList());

        public Task<int> CountByChatAsync(ChatId chatId, CancellationToken ct = default) =>
            Task.FromResult(_items.Count(x => x.ChatId.Value == chatId.Value));

        public Task<int> CountRecentBySenderAsync(ChatId chatId, Guid senderId, DateTime sinceUtc, CancellationToken ct = default) =>
            Task.FromResult(_items.Count(x => x.ChatId.Value == chatId.Value && x.SenderId == senderId && x.CreatedAt >= sinceUtc));

        public Task<Message?> FindRecentDuplicateAsync(ChatId chatId, Guid senderId, string normalizedText, DateTime sinceUtc, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.ChatId.Value == chatId.Value
                && x.SenderId == senderId
                && x.CreatedAt >= sinceUtc
                && string.Equals(x.Text.Trim(), normalizedText, StringComparison.OrdinalIgnoreCase)));

        public Task<int> CountUnreadForUserAsync(ChatId chatId, Guid userId, MessageId? afterMessageId, CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task<Message> AddAsync(Message entity, CancellationToken ct = default)
        {
            var saved = Message.Reconstitute(
                MessageId.From(_nextId++),
                entity.ChatId,
                entity.SenderId,
                entity.Text,
                entity.Attachments,
                entity.Status,
                entity.ReadAt,
                entity.DeletedBy,
                entity.ReplyToMessageId,
                entity.RawPayload,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.IsDeleted,
                entity.DeletedAt);
            _items.Add(saved);
            return Task.FromResult(saved);
        }

        public Task UpdateAsync(Message entity, CancellationToken ct = default) => Task.CompletedTask;
    }
}
