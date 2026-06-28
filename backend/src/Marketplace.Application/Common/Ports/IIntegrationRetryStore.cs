namespace Marketplace.Application.Common.Ports;

public interface IIntegrationRetryStore
{
    Task UpsertAsync(IntegrationRetryUpsert request, DateTime nextAttemptAtUtc, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationRetryEntry>> ListDueAsync(int batchSize, DateTime utcNow, CancellationToken ct = default);
    Task MarkResolvedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default);
    Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default);
}

public static class IntegrationRetryKinds
{
    public const string PaymentSync = "payment_sync";
    public const string InventoryExpire = "inventory_expire";
    public const string NotificationDispatch = "notification_dispatch";
}
