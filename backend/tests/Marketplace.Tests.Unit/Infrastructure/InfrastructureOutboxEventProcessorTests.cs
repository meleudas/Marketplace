using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Infrastructure.Jobs;

namespace Marketplace.Tests;

[Trait("Suite", "Platform")]
public sealed class InfrastructureOutboxEventProcessorTests
{
    [Fact]
    public async Task Duplicate_Message_Is_NoOp()
    {
        var inbox = new StubInboxDeduplicator(true);
        var orderRepo = new StubOrderRepository();
        var paymentRepo = new StubPaymentRepository();
        var processor = new OutboxEventProcessor(
            inbox,
            orderRepo,
            paymentRepo,
            new OrderPaymentStateApplier(),
            new StubOrderCacheInvalidation(),
            new StubOrderStatusHistoryWriter());

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "Payment",
            "1",
            "PaymentStatusChanged",
            "{\"messageId\":\"11111111-1111-1111-1111-111111111111\",\"paymentId\":1,\"orderId\":1,\"status\":\"Completed\"}",
            DateTime.UtcNow,
            null,
            0,
            null,
            null,
            null,
            null,
            null);

        await processor.ProcessAsync(message, CancellationToken.None);

        Assert.False(orderRepo.Updated);
        Assert.False(paymentRepo.Updated);
    }

    [Fact]
    public async Task PaymentStatusChanged_Updates_Payment_And_Order_And_Marks_Inbox()
    {
        var inbox = new StubInboxDeduplicator(false);
        var orderRepo = new StubOrderRepository();
        var paymentRepo = new StubPaymentRepository();
        var cache = new StubOrderCacheInvalidation();
        var history = new StubOrderStatusHistoryWriter();
        var processor = new OutboxEventProcessor(
            inbox,
            orderRepo,
            paymentRepo,
            new OrderPaymentStateApplier(),
            cache,
            history);

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "Payment",
            "1",
            "PaymentStatusChanged",
            "{\"messageId\":\"11111111-1111-1111-1111-111111111111\",\"paymentId\":1,\"orderId\":1,\"status\":\"Completed\"}",
            DateTime.UtcNow,
            null,
            0,
            null,
            null,
            null,
            null,
            null);

        await processor.ProcessAsync(message, CancellationToken.None);

        Assert.True(orderRepo.Updated);
        Assert.True(paymentRepo.Updated);
        Assert.True(cache.Invalidated);
        Assert.True(history.Wrote);
        Assert.True(inbox.MarkedProcessed);
    }

    [Fact]
    public async Task PaymentStatusChanged_Ignores_Downgrade()
    {
        var inbox = new StubInboxDeduplicator(false);
        var orderRepo = new StubOrderRepository(OrderStatus.Paid);
        var paymentRepo = new StubPaymentRepository(PaymentTransactionStatus.Completed);
        var processor = new OutboxEventProcessor(
            inbox,
            orderRepo,
            paymentRepo,
            new OrderPaymentStateApplier(),
            new StubOrderCacheInvalidation(),
            new StubOrderStatusHistoryWriter());

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "Payment",
            "1",
            "PaymentStatusChanged",
            "{\"messageId\":\"11111111-1111-1111-1111-111111111111\",\"paymentId\":1,\"orderId\":1,\"status\":\"Failed\"}",
            DateTime.UtcNow,
            null,
            0,
            null,
            null,
            null,
            null,
            null);

        await processor.ProcessAsync(message, CancellationToken.None);

        Assert.False(paymentRepo.Updated);
    }

    [Fact]
    public async Task Unsupported_EventType_Throws_PermanentOutboxException()
    {
        var processor = new OutboxEventProcessor(
            new StubInboxDeduplicator(false),
            new StubOrderRepository(),
            new StubPaymentRepository(),
            new OrderPaymentStateApplier(),
            new StubOrderCacheInvalidation(),
            new StubOrderStatusHistoryWriter());

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "Unknown",
            "1",
            "UnsupportedEvent",
            "{}",
            DateTime.UtcNow,
            null,
            0,
            null,
            null,
            null,
            null,
            null);

        await Assert.ThrowsAsync<PermanentOutboxException>(() => processor.ProcessAsync(message, CancellationToken.None));
    }

    private sealed class StubInboxDeduplicator : IInboxDeduplicator
    {
        private readonly bool _alwaysProcessed;
        public StubInboxDeduplicator(bool alwaysProcessed) => _alwaysProcessed = alwaysProcessed;
        public Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default) => Task.FromResult(_alwaysProcessed);
        public bool MarkedProcessed { get; private set; }
        public Task MarkProcessedAsync(Guid messageId, string consumer, string? metadata, CancellationToken ct = default)
        {
            MarkedProcessed = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubOrderRepository : IOrderRepository
    {
        private readonly Order _order;
        public StubOrderRepository(OrderStatus status = OrderStatus.Pending)
        {
            _order = BuildOrder(status);
        }

        public bool Updated { get; private set; }
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult<Order?>(_order);
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>([]);
        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default) => Task.FromResult(( (IReadOnlyList<Order>)[], 0L));
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) => Task.FromResult(order);
        public Task UpdateAsync(Order order, CancellationToken ct = default) { Updated = true; return Task.CompletedTask; }
        private static Order BuildOrder(OrderStatus status) => Order.Reconstitute(OrderId.From(1), "ORD-1", Guid.NewGuid(), CompanyId.From(Guid.NewGuid()), status, new Money(10), new Money(10), Money.Zero, Money.Zero, Money.Zero, ShippingMethodId.From(1), CheckoutPaymentMethod.Card, null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null);
    }

    private sealed class StubPaymentRepository : IPaymentRepository
    {
        private readonly Payment _payment;
        public StubPaymentRepository(PaymentTransactionStatus status = PaymentTransactionStatus.Pending)
        {
            _payment = BuildPayment(status);
        }

        public bool Updated { get; private set; }
        public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default) => Task.FromResult<Payment?>(_payment);
        public Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<Payment?>(_payment);
        public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default) => Task.FromResult<Payment?>(_payment);
        public Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Payment>>([]);
        public Task<Payment> AddAsync(Payment payment, CancellationToken ct = default) => Task.FromResult(payment);
        public Task UpdateAsync(Payment payment, CancellationToken ct = default) { Updated = true; return Task.CompletedTask; }
        private static Payment BuildPayment(PaymentTransactionStatus status) => Payment.Create(PaymentId.From(1), OrderId.From(1), PaymentMethodKind.LiqPay, new Money(10), "UAH", "trx", status, JsonBlob.Empty);
    }

    private sealed class StubOrderCacheInvalidation : IOrderCacheInvalidationService
    {
        public bool Invalidated { get; private set; }
        public Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default) => Task.FromResult(1L);
        public Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default)
        {
            Invalidated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubOrderStatusHistoryWriter : Marketplace.Application.Orders.Services.IOrderStatusHistoryWriter
    {
        public bool Wrote { get; private set; }
        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
        {
            Wrote = true;
            return Task.CompletedTask;
        }
    }
}
