using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public class DomainOrderStatusTransitionTests
{
    [Fact]
    public void Order_Transitions_To_Processing_Shipped_Delivered()
    {
        var order = BuildOrder(OrderStatus.Paid);

        order.SetProcessing();
        order.SetShipped("TRK-1");
        order.SetDelivered();

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.ShippedAt);
        Assert.NotNull(order.DeliveredAt);
    }

    [Fact]
    public void Order_Cancel_Rejects_From_Shipped()
    {
        var order = BuildOrder(OrderStatus.Shipped);
        Assert.ThrowsAny<Exception>(() => order.Cancel(OrderCancellationReasonCode.ChangedMind));
    }

    [Fact]
    public void Order_SetProcessing_Rejects_When_Status_Is_Delivered()
    {
        var order = BuildOrder(OrderStatus.Delivered);

        Assert.ThrowsAny<Exception>(() => order.SetProcessing());
    }

    [Fact]
    public void Order_SetShipped_Rejects_When_Status_Is_Not_Processing()
    {
        var order = BuildOrder(OrderStatus.Paid);

        Assert.ThrowsAny<Exception>(() => order.SetShipped("TRK-2"));
    }

    [Fact]
    public void Order_SetDelivered_Rejects_When_Status_Is_Not_Shipped()
    {
        var order = BuildOrder(OrderStatus.Processing);

        Assert.ThrowsAny<Exception>(() => order.SetDelivered());
    }

    [Fact]
    public void Order_Cancel_Allowed_From_Pending_And_Paid()
    {
        var fromPending = BuildOrder(OrderStatus.Pending);
        var fromPaid = BuildOrder(OrderStatus.Paid);

        fromPending.Cancel(OrderCancellationReasonCode.ChangedMind);
        fromPaid.Cancel(OrderCancellationReasonCode.ChangedMind);

        Assert.Equal(OrderStatus.Cancelled, fromPending.Status);
        Assert.Equal(OrderStatus.Cancelled, fromPaid.Status);
    }

    [Fact]
    public void Order_Cancel_Rejects_From_Terminal_Statuses()
    {
        var fromDelivered = BuildOrder(OrderStatus.Delivered);
        var fromCancelled = BuildOrder(OrderStatus.Cancelled);
        var fromRefunded = BuildOrder(OrderStatus.Refunded);

        Assert.ThrowsAny<Exception>(() => fromDelivered.Cancel(OrderCancellationReasonCode.FraudSuspected));
        Assert.ThrowsAny<Exception>(() => fromCancelled.Cancel(OrderCancellationReasonCode.ChangedMind));
        Assert.ThrowsAny<Exception>(() => fromRefunded.Cancel(OrderCancellationReasonCode.ChangedMind));
    }

    [Fact]
    public void Order_Admin_Can_Cancel_Shipped_With_Override()
    {
        var order = BuildOrder(OrderStatus.Shipped);
        order.Cancel(OrderCancellationReasonCode.FraudSuspected, "ops review", adminOverride: true);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(OrderCancellationReasonCode.FraudSuspected, order.CancellationReasonCode);
    }

    private static Order BuildOrder(OrderStatus status)
        => Order.Reconstitute(
            OrderId.From(1),
            "ORD-1",
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
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
}
