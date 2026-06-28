using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Support.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
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
        var processor = CreateProcessor(inbox, orderRepo, paymentRepo);

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
        var processor = CreateProcessor(inbox, orderRepo, paymentRepo, cache, history);

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
        var processor = CreateProcessor(inbox, orderRepo, paymentRepo);

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
        var processor = CreateProcessor(
            new StubInboxDeduplicator(false),
            new StubOrderRepository(),
            new StubPaymentRepository());

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

    [Fact]
    public async Task InventoryReleased_Invalidates_Catalog_Cache_For_Product()
    {
        var inbox = new StubInboxDeduplicator(false);
        var cache = new StubAppCachePort();
        var productRepo = new StubProductRepository();
        var processor = CreateProcessor(inbox, new StubOrderRepository(), new StubPaymentRepository(), productRepo: productRepo, appCache: cache);

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "InventoryReservation",
            "1",
            "InventoryReleased",
            "{\"messageId\":\"22222222-2222-2222-2222-222222222222\",\"productId\":42}",
            DateTime.UtcNow,
            null,
            0,
            null,
            null,
            null,
            null,
            null);

        await processor.ProcessAsync(message, CancellationToken.None);

        Assert.Contains(cache.RemovedKeys, x => x == CatalogCacheKeys.ProductList);
        Assert.Contains(cache.RemovedKeys, x => x == CatalogCacheKeys.ProductDetailPrefix + "product-42");
        Assert.True(inbox.MarkedProcessed);
    }

    private static OutboxEventProcessor CreateProcessor(
        StubInboxDeduplicator inbox,
        StubOrderRepository orderRepo,
        StubPaymentRepository paymentRepo,
        StubOrderCacheInvalidation? cache = null,
        StubOrderStatusHistoryWriter? history = null,
        StubProductRepository? productRepo = null,
        StubAppCachePort? appCache = null)
        => new(
            inbox,
            orderRepo,
            paymentRepo,
            new OrderPaymentStateApplier(),
            cache ?? new StubOrderCacheInvalidation(),
            history ?? new StubOrderStatusHistoryWriter(),
            new NoOpSupportHelpdeskSyncHandler(),
            productRepo ?? new StubProductRepository(),
            appCache ?? new StubAppCachePort());

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
        public Task RecordCreatedAsync(Order order, Guid actorUserId, string source, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
        {
            Wrote = true;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpSupportHelpdeskSyncHandler : ISupportHelpdeskSyncHandler
    {
        public Task ProcessAsync(OutboxMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubProductRepository : IProductRepository
    {
        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult<Product?>(Product.Reconstitute(
                id,
                CompanyId.From(Guid.NewGuid()),
                "Product",
                $"product-{id.Value}",
                "desc",
                new Money(10),
                null,
                1,
                0,
                CategoryId.From(1),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null));

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default) => Task.FromResult<Product?>(null);
        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult<Product?>(null);
        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task AddAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubAppCachePort : IAppCachePort
    {
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
