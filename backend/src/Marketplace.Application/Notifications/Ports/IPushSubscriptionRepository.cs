namespace Marketplace.Application.Notifications.Ports;

public sealed record PushSubscriptionDto(
    long Id,
    Guid UserId,
    string Endpoint,
    string P256dh,
    string Auth,
    PushSubscriptionAudienceFlags AudienceFlags);

public interface IPushSubscriptionRepository
{
    Task UpsertAsync(
        Guid userId,
        string endpoint,
        string p256dh,
        string auth,
        PushSubscriptionAudienceFlags audienceFlags,
        string? userAgent,
        CancellationToken ct = default);

    Task DeleteByUserAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default);

    Task<IReadOnlyList<PushSubscriptionDto>> ListByUserAndAudienceAsync(
        Guid userId,
        PushSubscriptionAudienceFlags requiredFlags,
        CancellationToken ct = default);

    Task<IReadOnlyList<PushSubscriptionDto>> ListByAudienceFlagAsync(
        PushSubscriptionAudienceFlags requiredFlag,
        CancellationToken ct = default);

    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
