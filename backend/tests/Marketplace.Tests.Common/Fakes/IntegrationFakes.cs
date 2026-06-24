using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Coupons.Validation;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Tests.Common.Fakes;

public sealed class NoopCachePort : IAppCachePort
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class NoopOrderStatusHistoryWriter : IOrderStatusHistoryWriter
{
    public Task RecordCreatedAsync(Order order, Guid actorUserId, string source, string? correlationId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
        => Task.CompletedTask;
}

public sealed class NoopAppNotificationScheduler : IAppNotificationScheduler
{
    public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class NoopCartStockWatchRepository : ICartStockWatchRepository
{
    public Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(long productId, TimeSpan minIntervalSinceLastNotify, DateTime utcNow, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>([]);
    public Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class FakeLiqPayPort : ILiqPayPort
{
    public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(new LiqPayCreatePaymentResult(true, request.OrderNumber, "https://liqpay.test", "data", "sig", "{\"status\":\"ok\"}", null));

    public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default) => Task.FromResult(true);

    public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
        => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, "success", "{}", null));

    public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default)
        => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));

    public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default)
        => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));

    public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
}

public sealed class ThrowingOutboxWriter : IOutboxWriter
{
    public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
        => throw new InvalidOperationException("simulated outbox failure");

    public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

    public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
        => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
        => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
}

public sealed class NoopCouponCheckoutService : ICouponCheckoutService
{
    public Task<CheckoutCouponPlanResult> ResolveCheckoutPlanAsync(Guid actorUserId, CartId cartId, IReadOnlyList<CouponCartLine> lines, CancellationToken ct = default)
        => Task.FromResult(CheckoutCouponPlanResult.Empty);

    public Task<CheckoutCouponRevalidationResult> RevalidateForCheckoutAsync(Guid actorUserId, CartId cartId, IReadOnlyList<Domain.Cart.Entities.CartItem> items, IReadOnlyDictionary<long, Domain.Catalog.Entities.Product> productMap, CancellationToken ct = default)
        => Task.FromResult(CheckoutCouponRevalidationResult.Valid());

    public Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(Guid actorUserId, CartId cartId, CompanyId companyId, decimal subtotal, CancellationToken ct = default)
        => Task.FromResult(new CheckoutCouponDiscountResult(0, null, null));

    public Task ConsumeOnceAsync(Guid actorUserId, OrderId primaryOrderId, CartId cartId, long couponId, string couponCode, decimal totalDiscountAmount, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ConsumeAsync(Guid actorUserId, OrderId orderId, long couponId, string couponCode, decimal discountAmount, CancellationToken ct = default)
        => Task.CompletedTask;
}
