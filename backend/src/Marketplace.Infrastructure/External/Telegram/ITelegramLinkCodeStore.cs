namespace Marketplace.Infrastructure.External.Telegram;

public interface ITelegramLinkCodeStore
{
    Task StoreAsync(string code, Guid identityUserId, TimeSpan ttl, CancellationToken ct = default);

    Task<Guid?> TakeAsync(string code, CancellationToken ct = default);
}
