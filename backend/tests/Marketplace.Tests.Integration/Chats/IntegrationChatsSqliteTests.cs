using Marketplace.Application.Chats.Commands.ArchiveChat;
using Marketplace.Application.Chats.Commands.CreateChat;
using Marketplace.Application.Chats.Commands.MarkMessageRead;
using Marketplace.Application.Chats.Commands.SendMessage;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Application.Chats.Ports;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Chats")]
public sealed class IntegrationChatsSqliteTests
{
    [Fact]
    public async Task Support_Chat_Send_Read_Archive_Flow_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var chatRepo = new ChatRepository(db);
        var participantRepo = new ChatParticipantRepository(db);
        var messageRepo = new MessageRepository(db);
        var readStateRepo = new ChatReadStateRepository(db);
        var options = Options.Create(new ChatsOptions
        {
            Enabled = true,
            RealtimeEnabled = false,
            ModerationEnabled = false,
            MessagesPerMinute = 30,
            DuplicateWindowSeconds = 1
        });

        var buyer = Guid.NewGuid();
        var create = new CreateChatCommandHandler(
            chatRepo,
            participantRepo,
            new StubProductRepository(),
            new StubOrderRepository(),
            new StubCompanyMemberRepository(),
            options);
        var access = new ChatAccessPolicy(participantRepo, chatRepo);
        var antiSpam = new ChatAntiSpamPolicy(messageRepo, options);
        var content = new ChatContentModerationPolicy(options);
        var notifications = new StubNotificationScheduler();
        var realtime = new StubRealtimeNotifier();

        var created = await create.Handle(new CreateChatCommand(buyer, (short)ChatType.Support, null, null), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var send = new SendMessageCommandHandler(
            chatRepo,
            messageRepo,
            participantRepo,
            access,
            antiSpam,
            content,
            notifications,
            realtime,
            options);
        var sent = await send.Handle(new SendMessageCommand(buyer, created.Value!.Id, "hello support", null, false), CancellationToken.None);
        Assert.True(sent.IsSuccess);

        var markRead = new MarkMessageReadCommandHandler(chatRepo, messageRepo, readStateRepo, access, realtime, options);
        var read = await markRead.Handle(new MarkMessageReadCommand(buyer, created.Value.Id, sent.Value!.Id, false), CancellationToken.None);
        Assert.True(read.IsSuccess);

        var archive = new ArchiveChatCommandHandler(chatRepo, readStateRepo, messageRepo, access, realtime, options);
        var archived = await archive.Handle(new ArchiveChatCommand(buyer, created.Value.Id, false), CancellationToken.None);
        Assert.True(archived.IsSuccess);
        Assert.Equal((short)ChatStatus.Archived, archived.Value!.Status);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class StubProductRepository : Marketplace.Domain.Catalog.Repositories.IProductRepository
    {
        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetByIdAsync(Marketplace.Domain.Common.ValueObjects.ProductId id, CancellationToken ct = default) =>
            Task.FromResult<Marketplace.Domain.Catalog.Entities.Product?>(null);
        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetBySlugAsync(Marketplace.Domain.Common.ValueObjects.CompanyId companyId, string slug, CancellationToken ct = default) =>
            Task.FromResult<Marketplace.Domain.Catalog.Entities.Product?>(null);
        public Task<Marketplace.Domain.Catalog.Entities.Product?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult<Marketplace.Domain.Catalog.Entities.Product?>(null);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListByIdsAsync(IReadOnlyCollection<Marketplace.Domain.Common.ValueObjects.ProductId> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListByCompanyAsync(Marketplace.Domain.Common.ValueObjects.CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListActiveOnSaleAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minDiscountPercent = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListActiveNewestAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListActivePopularAsync(Guid? companyId = null, IReadOnlyList<long>? categoryIds = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>> ListPendingReviewAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Catalog.Entities.Product>>([]);
        public Task AddAsync(Marketplace.Domain.Catalog.Entities.Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Catalog.Entities.Product product, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderRepository : Marketplace.Domain.Orders.Repositories.IOrderRepository
    {
        public Task<Marketplace.Domain.Orders.Entities.Order?> GetByIdAsync(Marketplace.Domain.Common.ValueObjects.OrderId id, CancellationToken ct = default) =>
            Task.FromResult<Marketplace.Domain.Orders.Entities.Order?>(null);
        public Task<IReadOnlyList<Marketplace.Domain.Orders.Entities.Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Orders.Entities.Order>>([]);
        public Task<(IReadOnlyList<Marketplace.Domain.Orders.Entities.Order> Items, long Total)> ListAsync(Marketplace.Domain.Orders.Repositories.OrderListFilter filter, CancellationToken ct = default) =>
            Task.FromResult(((IReadOnlyList<Marketplace.Domain.Orders.Entities.Order>)Array.Empty<Marketplace.Domain.Orders.Entities.Order>(), 0L));
        public Task<Marketplace.Domain.Orders.Entities.Order> AddAsync(Marketplace.Domain.Orders.Entities.Order order, CancellationToken ct = default) =>
            Task.FromResult(order);
        public Task UpdateAsync(Marketplace.Domain.Orders.Entities.Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubCompanyMemberRepository : Marketplace.Domain.Companies.Repositories.ICompanyMemberRepository
    {
        public Task<Marketplace.Domain.Companies.Entities.CompanyMember?> GetByCompanyAndUserAsync(Marketplace.Domain.Common.ValueObjects.CompanyId companyId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult<Marketplace.Domain.Companies.Entities.CompanyMember?>(null);
        public Task<IReadOnlyList<Marketplace.Domain.Companies.Entities.CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Companies.Entities.CompanyMember>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Companies.Entities.CompanyMember>> ListByCompanyAsync(Marketplace.Domain.Common.ValueObjects.CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Marketplace.Domain.Companies.Entities.CompanyMember>>([]);
        public Task<bool> ExistsOwnerAsync(Marketplace.Domain.Common.ValueObjects.CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult(false);
        public Task AddAsync(Marketplace.Domain.Companies.Entities.CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Marketplace.Domain.Companies.Entities.CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubNotificationScheduler : IAppNotificationScheduler
    {
        public Task ScheduleAsync(Marketplace.Application.Notifications.AppNotificationRequest request, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class StubRealtimeNotifier : IChatRealtimeNotifier
    {
        public Task NotifyMessageReceivedAsync(Guid chatId, long messageId, Guid senderId, string text, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task NotifyMessageReadAsync(Guid chatId, Guid userId, long messageId, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task NotifyChatArchivedAsync(Guid chatId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
