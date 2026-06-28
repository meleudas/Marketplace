namespace Marketplace.Application.Chats.Ports;

public interface IChatRealtimeNotifier
{
    Task NotifyMessageReceivedAsync(Guid chatId, long messageId, Guid senderId, string text, CancellationToken ct = default);
    Task NotifyMessageReadAsync(Guid chatId, Guid userId, long messageId, CancellationToken ct = default);
    Task NotifyChatArchivedAsync(Guid chatId, CancellationToken ct = default);
}
