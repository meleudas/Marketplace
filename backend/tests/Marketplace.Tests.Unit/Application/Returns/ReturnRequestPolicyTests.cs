using Marketplace.Application.Returns.Options;
using Marketplace.Application.Returns.Policies;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Returns.Enums;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Returns")]
public sealed class ReturnRequestPolicyTests
{
    private readonly ReturnRequestPolicy _policy = new(Options.Create(new ReturnRequestOptions()));

    [Fact]
    public void Buyer_Can_Request_For_Delivered_Within_Window()
    {
        var order = BuildOrder(OrderStatus.Delivered);
        var result = _policy.ValidateRequest(
            order,
            ReturnReasonCode.DamagedInShipping,
            null,
            DateTime.UtcNow,
            new Dictionary<long, int>(),
            new Dictionary<long, int> { [10] = 2 },
            [(10, 1)]);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Buyer_Cannot_Request_After_Window()
    {
        var order = BuildOrder(OrderStatus.Delivered, DateTime.UtcNow.AddDays(-30));
        var result = _policy.ValidateRequest(
            order,
            ReturnReasonCode.DamagedInShipping,
            null,
            DateTime.UtcNow,
            new Dictionary<long, int>(),
            new Dictionary<long, int> { [10] = 2 },
            [(10, 1)]);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Partial_Return_Qty_Cannot_Exceed_Remaining()
    {
        var order = BuildOrder(OrderStatus.Delivered);
        var result = _policy.ValidateRequest(
            order,
            ReturnReasonCode.WrongItem,
            null,
            DateTime.UtcNow,
            new Dictionary<long, int> { [10] = 1 },
            new Dictionary<long, int> { [10] = 2 },
            [(10, 2)]);
        Assert.True(result.IsFailure);
    }

    private static Order BuildOrder(OrderStatus status, DateTime? deliveredAt = null) =>
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
            null,
            null,
            null,
            deliveredAt,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
}
