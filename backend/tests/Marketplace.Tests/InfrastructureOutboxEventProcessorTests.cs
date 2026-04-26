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
            null);

        await processor.ProcessAsync(message, CancellationToken.None);

        Assert.False(orderRepo.Updated);
        Assert.False(paymentRepo.Updated);
    }

    private sealed class StubInboxDeduplicator : IInboxDeduplicator
    {
        private readonly bool _alwaysProcessed;
        public StubInboxDeduplicator(bool alwaysProcessed) => _alwaysProcessed = alwaysProcessed;
        public Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default) => Task.FromResult(_alwaysProcessed);
        public Task MarkProcessedAsync(Guid messageId, string consumer, string? metadata, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderRepository : IOrderRepository
    {
        public bool Updated { get; private set; }
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult<Order?>(BuildOrder());
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>([]);
        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default) => Task.FromResult(( (IReadOnlyList<Order>)[], 0L));
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) => Task.FromResult(order);
        public Task UpdateAsync(Order order, CancellationToken ct = default) { Updated = true; return Task.CompletedTask; }
        private static Order BuildOrder() => Order.Reconstitute(OrderId.From(1), "ORD-1", Guid.NewGuid(), CompanyId.From(Guid.NewGuid()), OrderStatus.Pending, new Money(10), new Money(10), Money.Zero, Money.Zero, Money.Zero, ShippingMethodId.From(1), CheckoutPaymentMethod.Card, null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null);
    }

    private sealed class StubPaymentRepository : IPaymentRepository
    {
        public bool Updated { get; private set; }
        public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default) => Task.FromResult<Payment?>(BuildPayment());
        public Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<Payment?>(BuildPayment());
        public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default) => Task.FromResult<Payment?>(BuildPayment());
        public Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Payment>>([]);
        public Task<Payment> AddAsync(Payment payment, CancellationToken ct = default) => Task.FromResult(payment);
        public Task UpdateAsync(Payment payment, CancellationToken ct = default) { Updated = true; return Task.CompletedTask; }
        private static Payment BuildPayment() => Payment.Create(PaymentId.From(1), OrderId.From(1), PaymentMethodKind.LiqPay, new Money(10), "UAH", "trx", PaymentTransactionStatus.Pending, JsonBlob.Empty);
    }

    private sealed class StubOrderCacheInvalidation : IOrderCacheInvalidationService
    {
        public Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default) => Task.FromResult(1L);
        public Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderStatusHistoryWriter : Marketplace.Application.Orders.Services.IOrderStatusHistoryWriter
    {
        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
