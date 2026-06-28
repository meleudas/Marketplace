using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests.Common.Fakes;

public sealed class NoopCheckoutInventoryService : ICheckoutInventoryService
{
    public Task ReserveForOrderAsync(
        OrderId orderId,
        CompanyId companyId,
        IReadOnlyList<(OrderItemId OrderItemId, ProductId ProductId, int Quantity)> lines,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ConfirmForOrderAsync(OrderId orderId, CompanyId companyId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ReleaseForOrderAsync(
        OrderId orderId,
        CompanyId companyId,
        Guid? actorUserId,
        string reason,
        CancellationToken ct = default)
        => Task.CompletedTask;
}

public static class OrderTestDoubles
{
    public static OrderMutationCoordinator CreateCoordinator(IOrderCacheInvalidationService cache, IOutboxWriter outbox)
        => new(cache, outbox);
}

public sealed class VersionTrackingCachePort : IAppCachePort
{
    private readonly Dictionary<string, object> _items = new(StringComparer.Ordinal);

    public List<string> RemovedKeys { get; } = [];
    public Dictionary<string, long> VersionValues { get; } = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult(_items.TryGetValue(key, out var value) ? value as T : null);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        _items[key] = value!;
        if (key.StartsWith("orders:list:version:", StringComparison.Ordinal))
        {
            var prop = value.GetType().GetProperty("Value");
            if (prop?.GetValue(value) is long version)
                VersionValues[key] = version;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        RemovedKeys.Add(key);
        _items.Remove(key);
        return Task.CompletedTask;
    }
}
