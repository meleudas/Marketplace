using Marketplace.Application.Chats.Ports;

namespace Marketplace.Infrastructure.Chats;

public sealed class NullChatRealtimeNotifier : IChatRealtimeNotifier
{
    public Task NotifyMessageReceivedAsync(Guid chatId, long messageId, Guid senderId, string text, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyMessageReadAsync(Guid chatId, Guid userId, long messageId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyChatArchivedAsync(Guid chatId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
