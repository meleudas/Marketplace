using System.Text;
using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Marketplace.Tests.Payments;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Payments")]
[Trait("Layer", "IntegrationContainers")]
public sealed class LiqPayWebhookPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public LiqPayWebhookPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Webhook_With_Postgres_Is_Idempotent_And_Persists_State()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var orderRepo = new OrderRepository(db);
        var paymentRepo = new PaymentRepository(db);
        var orderNumber = $"ORD-PG-PAY-{Guid.NewGuid():N}"[..24];
        var order = await orderRepo.AddAsync(BuildOrder(orderNumber, OrderStatus.Pending), CancellationToken.None);
        var payment = await paymentRepo.AddAsync(
            Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(150), "UAH", orderNumber, PaymentTransactionStatus.Pending, JsonBlob.Empty),
            CancellationToken.None);

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"order_id\":\"{orderNumber}\",\"status\":\"success\"}}"));
        var handler = new HandleLiqPayWebhookCommandHandler(
            new FakeLiqPayPort(),
            paymentRepo,
            orderRepo,
            new Marketplace.Application.Orders.Cache.OrderCacheInvalidationService(new NoopCachePort()),
            new OrderPaymentStateApplier(),
            new OutboxRepository(db),
            new Marketplace.Application.Orders.Services.OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            new InboxDeduplicator(db),
            new NoopAppNotificationScheduler());

        var first = await handler.Handle(new HandleLiqPayWebhookCommand(payload, "sig-1", "idem-pg-a"), CancellationToken.None);
        var second = await handler.Handle(new HandleLiqPayWebhookCommand(payload, "sig-1", "idem-pg-b"), CancellationToken.None);

        var savedPayment = await paymentRepo.GetByIdAsync(payment.Id, CancellationToken.None);
        var savedOrder = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var paymentAggregateId = payment.Id.Value.ToString();
        var inboxRows = await db.InboxMessages.AsNoTracking()
            .Where(x => x.Consumer == "liqpay-webhook" && x.Metadata == $"transactionId={orderNumber}")
            .ToListAsync();
        var outboxRows = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.AggregateType == "Payment" && x.AggregateId == paymentAggregateId)
            .ToListAsync();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotNull(savedPayment);
        Assert.NotNull(savedOrder);
        Assert.Equal(PaymentTransactionStatus.Completed, savedPayment!.Status);
        Assert.Equal(OrderStatus.Paid, savedOrder!.Status);
        Assert.Single(inboxRows);
        Assert.Single(outboxRows);
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
}
