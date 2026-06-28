using Marketplace.Application.Orders.Options;
using Marketplace.Application.Orders.Policies;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class OrderCancellationPolicyTests
{
    private readonly OrderCancellationPolicy _policy = new(Options.Create(new OrderCancellationOptions()));

    [Fact]
    public void Buyer_Can_Cancel_Paid_Within_24h()
    {
        var order = BuildOrder(OrderStatus.Paid, DateTime.UtcNow.AddHours(-2));
        var result = _policy.Validate(order, OrderCancellationActor.Buyer, OrderCancellationReasonCode.ChangedMind, null, DateTime.UtcNow);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Buyer_Cannot_Cancel_Paid_After_24h()
    {
        var order = BuildOrder(OrderStatus.Paid, DateTime.UtcNow.AddHours(-25));
        var result = _policy.Validate(order, OrderCancellationActor.Buyer, OrderCancellationReasonCode.ChangedMind, null, DateTime.UtcNow);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Seller_Can_Cancel_Processing_Within_Window()
    {
        var order = BuildOrder(OrderStatus.Processing, DateTime.UtcNow.AddHours(-10));
        var result = _policy.Validate(order, OrderCancellationActor.CompanyMember, OrderCancellationReasonCode.OutOfStock, null, DateTime.UtcNow);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Buyer_Cannot_Cancel_Processing()
    {
        var order = BuildOrder(OrderStatus.Processing, DateTime.UtcNow);
        var result = _policy.Validate(order, OrderCancellationActor.Buyer, OrderCancellationReasonCode.ChangedMind, null, DateTime.UtcNow);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Admin_Can_Override_Shipped_With_FraudReason()
    {
        var order = BuildOrder(OrderStatus.Shipped, DateTime.UtcNow.AddDays(-5));
        var result = _policy.Validate(order, OrderCancellationActor.Admin, OrderCancellationReasonCode.FraudSuspected, "ops", DateTime.UtcNow);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Other_Reason_Requires_Comment_When_Configured()
    {
        var order = BuildOrder(OrderStatus.Pending, DateTime.UtcNow);
        var result = _policy.Validate(order, OrderCancellationActor.Buyer, OrderCancellationReasonCode.Other, null, DateTime.UtcNow);
        Assert.True(result.IsFailure);
    }

    private static Order BuildOrder(OrderStatus status, DateTime createdAt) =>
        Order.Reconstitute(
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
            null, null, null, null, null, null,
            createdAt,
            createdAt,
            false,
            null);
}
