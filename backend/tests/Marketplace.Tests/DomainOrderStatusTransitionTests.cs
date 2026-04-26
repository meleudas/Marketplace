using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Tests;

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
        Assert.ThrowsAny<Exception>(() => order.Cancel());
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
