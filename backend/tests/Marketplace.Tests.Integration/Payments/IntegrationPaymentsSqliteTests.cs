using System.Text;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Application.Payments.Commands.RequestRefund;
using Marketplace.Application.Payments.Commands.SyncPaymentStatus;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Marketplace.Tests.Common.Fakes;

namespace Marketplace.Tests;

[Trait("Suite", "Payments")]
public sealed class IntegrationPaymentsSqliteTests
{
    [Fact]
    public async Task Webhook_With_Sqlite_Is_Idempotent_And_Persists_State()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var paymentRepo = new PaymentRepository(db);
        var order = await orderRepo.AddAsync(BuildOrder("ORD-SQL-PAY-1", OrderStatus.Pending), CancellationToken.None);
        var payment = await paymentRepo.AddAsync(
            Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(150), "UAH", "ORD-SQL-PAY-1", PaymentTransactionStatus.Pending, JsonBlob.Empty),
            CancellationToken.None);

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"order_id\":\"ORD-SQL-PAY-1\",\"status\":\"success\"}"));
        var handler = new HandleLiqPayWebhookCommandHandler(
            new FakeLiqPayPort(),
            paymentRepo,
            orderRepo,
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
            new OrderPaymentStateApplier(),
            new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            new InboxDeduplicator(db),
            new NoopAppNotificationScheduler(),
            new NoopCheckoutInventoryService(),
            new NoopOrderFinancialsWriter());

        var first = await handler.Handle(new HandleLiqPayWebhookCommand(payload, "sig-1", "idem-a"), CancellationToken.None);
        var second = await handler.Handle(new HandleLiqPayWebhookCommand(payload, "sig-1", "idem-b"), CancellationToken.None);

        var savedPayment = await paymentRepo.GetByIdAsync(payment.Id, CancellationToken.None);
        var savedOrder = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var inboxRows = await db.InboxMessages.AsNoTracking().ToListAsync();
        var outboxRows = await db.OutboxMessages.AsNoTracking().ToListAsync();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotNull(savedPayment);
        Assert.NotNull(savedOrder);
        Assert.Equal(PaymentTransactionStatus.Completed, savedPayment!.Status);
        Assert.Equal(OrderStatus.Paid, savedOrder!.Status);
        Assert.Single(inboxRows);
        Assert.Single(outboxRows);
    }

    [Fact]
    public async Task RequestRefund_With_Sqlite_Persists_Refund_And_Updates_Order()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var paymentRepo = new PaymentRepository(db);
        var refundRepo = new RefundRepository(db);
        var order = await orderRepo.AddAsync(BuildOrder("ORD-SQL-PAY-2", OrderStatus.Paid), CancellationToken.None);
        var payment = await paymentRepo.AddAsync(
            Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(220), "UAH", "ORD-SQL-PAY-2", PaymentTransactionStatus.Completed, JsonBlob.Empty),
            CancellationToken.None);

        var handler = new RequestRefundCommandHandler(new PaymentRefundExecutor(
            paymentRepo,
            refundRepo,
            orderRepo,
            new FakeLiqPayPort(),
            new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            new OrderPaymentStateApplier(),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
            new NoopOrderFinancialsWriter()));

        var result = await handler.Handle(new RequestRefundCommand(payment.Id.Value, 120m, "manual refund", Guid.NewGuid()), CancellationToken.None);

        var savedOrder = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var refunds = await refundRepo.ListByOrderIdAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Refunded, savedOrder!.Status);
        Assert.Single(refunds);
        Assert.Equal(120m, refunds[0].Amount.Amount);
    }

    [Fact]
    public async Task SyncPaymentStatus_With_Sqlite_Ignores_Downgrade()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var paymentRepo = new PaymentRepository(db);
        var order = await orderRepo.AddAsync(BuildOrder("ORD-SQL-PAY-3", OrderStatus.Paid), CancellationToken.None);
        var payment = await paymentRepo.AddAsync(
            Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(90), "UAH", "ORD-SQL-PAY-3", PaymentTransactionStatus.Completed, JsonBlob.Empty),
            CancellationToken.None);

        var handler = new SyncPaymentStatusCommandHandler(
            paymentRepo,
            orderRepo,
            new FakeLiqPayPort { StatusResponse = "failure" },
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
            new OrderPaymentStateApplier(),
            new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            new NoopCheckoutInventoryService());

        var result = await handler.Handle(new SyncPaymentStatusCommand(payment.Id.Value), CancellationToken.None);
        var savedPayment = await paymentRepo.GetByIdAsync(payment.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentTransactionStatus.Completed, savedPayment!.Status);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Order BuildOrder(string orderNumber, OrderStatus status)
    {
        var now = DateTime.UtcNow;
        return Order.Reconstitute(
            OrderId.From(0),
            orderNumber,
            Guid.NewGuid(),
            CompanyId.From(Guid.NewGuid()),
            status,
            new Money(100),
            new Money(100),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(1),
            CheckoutPaymentMethod.Card,
            null,
            null,
            null,
            null,
            null,
            null,
            now,
            now,
            false,
            null);
    }

    private sealed class FakeLiqPayPort : ILiqPayPort
    {
        public bool VerifySignatureResult { get; set; } = true;
        public string StatusResponse { get; set; } = "success";

        public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayCreatePaymentResult(true, request.OrderNumber, "https://liqpay.test", "data", "signature", "{}", null));

        public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default)
            => Task.FromResult(VerifySignatureResult);

        public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
            => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, StatusResponse, "{}", null));

        public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));

        public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default)
            => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));

        public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopAppNotificationScheduler : IAppNotificationScheduler
    {
        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
    }
}
