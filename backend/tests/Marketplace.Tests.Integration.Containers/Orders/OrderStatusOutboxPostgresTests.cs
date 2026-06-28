using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Policies;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Orders;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Orders")]
[Trait("Layer", "IntegrationContainers")]
public sealed class OrderStatusOutboxPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public OrderStatusOutboxPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UpdateStatus_With_Postgres_Schedules_Notification_And_Outbox()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderRepo = new OrderRepository(db);
        var notificationSpy = new SpyAppNotificationScheduler();
        var customerId = Guid.NewGuid();

        var order = await orderRepo.AddAsync(BuildOrder(OrderStatus.Paid, customerId), CancellationToken.None);
        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAccessService(),
            new NoopShipmentFulfillmentService(),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
            new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            notificationSpy);

        var toProcessing = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, customerId, false, OrderStatus.Processing, null, "idem-pg-order-processing"),
            CancellationToken.None);
        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, customerId, false, OrderStatus.Shipped, "TRK-1", "idem-pg-order-shipped"),
            CancellationToken.None);

        var outboxRows = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.AggregateType == "Order" && x.AggregateId == order.Id.Value.ToString())
            .ToListAsync();

        Assert.True(toProcessing.IsSuccess);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, outboxRows.Count);
        Assert.Equal(2, notificationSpy.Requests.Count);
    }

    private static Order BuildOrder(OrderStatus status, Guid customerId)
    {
        var now = DateTime.UtcNow;
        return Order.Reconstitute(
            OrderId.From(0),
            $"ORD-PG-{Guid.NewGuid():N}"[..20],
            customerId,
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

    private sealed class AllowAccessService : IOrderAccessService
    {
        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<OrderCancellationActor> ResolveCancellationActorAsync(Order order, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(isActorAdmin ? OrderCancellationActor.Admin : OrderCancellationActor.CompanyMember);
    }

    private sealed class SpyAppNotificationScheduler : IAppNotificationScheduler
    {
        public List<AppNotificationRequest> Requests { get; } = [];

        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}
