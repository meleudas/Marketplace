using Marketplace.Infrastructure.Caching;

namespace Marketplace.Infrastructure.External.Telegram;

public sealed class TelegramLinkCodeStore : ITelegramLinkCodeStore
{
    private readonly ICacheService _cache;

    public TelegramLinkCodeStore(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task StoreAsync(string code, Guid identityUserId, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = BuildKey(code);
        var value = new LinkCodePayload(identityUserId);
        await _cache.SetAsync(key, value, ttl, ct);
    }

    public async Task<Guid?> TakeAsync(string code, CancellationToken ct = default)
    {
        var key = BuildKey(code);
        var payload = await _cache.GetAsync<LinkCodePayload>(key, ct);
        if (payload is null)
            return null;

        await _cache.RemoveAsync(key, ct);
        return payload.IdentityUserId;
    }

    private static string BuildKey(string code) => $"tg:link:{code.ToUpperInvariant()}";

    private sealed record LinkCodePayload(Guid IdentityUserId);
}
