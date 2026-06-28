using Marketplace.Application.Orders.Options;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Orders.Policies;

public sealed class OrderCancellationPolicy
{
    private readonly OrderCancellationOptions _options;

    public OrderCancellationPolicy(IOptions<OrderCancellationOptions> options)
    {
        _options = options.Value;
    }

    public Result Validate(
        Order order,
        OrderCancellationActor actor,
        OrderCancellationReasonCode reasonCode,
        string? comment,
        DateTime utcNow)
    {
        if (_options.RequireCommentForOther
            && reasonCode == OrderCancellationReasonCode.Other
            && string.IsNullOrWhiteSpace(comment))
        {
            return Result.Failure("Comment is required when reason is Other");
        }

        if (order.Status is OrderStatus.Refunded or OrderStatus.Cancelled)
            return Result.Failure("Order cannot be cancelled");

        return actor switch
        {
            OrderCancellationActor.Admin => ValidateAdmin(order, reasonCode),
            OrderCancellationActor.Buyer => ValidateBuyer(order, utcNow),
            OrderCancellationActor.CompanyMember => ValidateSeller(order, utcNow),
            _ => Result.Failure("Invalid cancellation actor")
        };
    }

    private Result ValidateBuyer(Order order, DateTime utcNow)
    {
        return order.Status switch
        {
            OrderStatus.Pending when utcNow <= order.CreatedAt.AddMinutes(_options.BuyerPendingWindowMinutes)
                => Result.Success(),
            OrderStatus.Paid when utcNow <= order.CreatedAt.AddHours(_options.BuyerPaidWindowHours)
                => Result.Success(),
            OrderStatus.Pending or OrderStatus.Paid
                => Result.Failure("Cancellation window has expired"),
            _ => Result.Failure("Buyer cannot cancel order in current status")
        };
    }

    private Result ValidateSeller(Order order, DateTime utcNow)
    {
        return order.Status switch
        {
            OrderStatus.Pending or OrderStatus.Paid => Result.Success(),
            OrderStatus.Processing when utcNow <= order.CreatedAt.AddHours(_options.SellerProcessingWindowHours)
                => Result.Success(),
            OrderStatus.Processing => Result.Failure("Seller cancellation window has expired"),
            _ => Result.Failure("Seller cannot cancel order in current status")
        };
    }

    private static Result ValidateAdmin(Order order, OrderCancellationReasonCode reasonCode)
    {
        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered
            && reasonCode is not (OrderCancellationReasonCode.FraudSuspected or OrderCancellationReasonCode.Other))
        {
            return Result.Failure("Admin must use FraudSuspected or Other to cancel shipped/delivered orders");
        }

        return Result.Success();
    }
}
