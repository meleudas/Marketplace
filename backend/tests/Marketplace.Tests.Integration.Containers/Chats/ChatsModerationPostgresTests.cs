using Marketplace.Application.Chats.Commands.ModerateChatMessage;
using Marketplace.Application.Chats.Options;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Chats")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ChatsModerationPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ChatsModerationPostgresTests(MarketplaceContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Moderation_Action_Is_Persisted()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var chatRepo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        var participantRepo = scope.ServiceProvider.GetRequiredService<IChatParticipantRepository>();
        var messageRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        var moderationRepo = scope.ServiceProvider.GetRequiredService<IChatModerationActionRepository>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ChatsOptions>>();

        if (!options.Value.Enabled)
            return;

        var buyer = Guid.NewGuid();
        var moderator = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var chat = await chatRepo.AddAsync(Chat.CreateSupport(buyer, now), CancellationToken.None);
        await participantRepo.AddAsync(ChatParticipant.Join(chat.Id, buyer, ChatParticipantRole.Buyer, null, now), CancellationToken.None);
        var message = await messageRepo.AddAsync(
            Message.Send(chat.Id, buyer, "needs review", JsonBlob.Empty, null, now),
            CancellationToken.None);

        var handler = new ModerateChatMessageCommandHandler(chatRepo, messageRepo, moderationRepo, options);
        var result = await handler.Handle(
            new ModerateChatMessageCommand(
                moderator,
                chat.Id.Value,
                message.Id.Value,
                (short)ChatModerationActionType.Hide,
                "policy"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var actions = await moderationRepo.ListByChatAsync(chat.Id, 10, CancellationToken.None);
        Assert.Single(actions);
    }
}
