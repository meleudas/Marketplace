using Marketplace.Application.Returns.Options;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Returns.Policies;

public sealed class ReturnRequestPolicy
{
    private readonly ReturnRequestOptions _options;

    public ReturnRequestPolicy(IOptions<ReturnRequestOptions> options) => _options = options.Value;

    public Result ValidateRequest(
        Order order,
        ReturnReasonCode reasonCode,
        string? comment,
        DateTime utcNow,
        IReadOnlyDictionary<long, int> alreadyReturnedByItem,
        IReadOnlyDictionary<long, int> orderItemQuantities,
        IReadOnlyList<(long OrderItemId, int Quantity)> requestedLines)
    {
        if (_options.RequireCommentForOther && reasonCode == ReturnReasonCode.Other && string.IsNullOrWhiteSpace(comment))
            return Result.Failure("Comment is required when reason is Other");

        if (order.Status == OrderStatus.Delivered)
        {
            if (order.DeliveredAt.HasValue && utcNow > order.DeliveredAt.Value.AddDays(_options.MaxDaysAfterDelivery))
                return Result.Failure("Return window has expired");
        }
        else if (order.Status == OrderStatus.Shipped && _options.AllowReturnWhileShipped)
        {
            if (order.ShippedAt.HasValue && utcNow > order.ShippedAt.Value.AddHours(_options.ShippedReturnWindowHours))
                return Result.Failure("Return window has expired");
        }
        else
        {
            return Result.Failure("Returns are only allowed for delivered orders");
        }

        if (requestedLines.Count == 0)
            return Result.Failure("At least one return line is required");

        foreach (var (orderItemId, qty) in requestedLines)
        {
            if (qty <= 0)
                return Result.Failure($"Invalid quantity for order item {orderItemId}");
            if (!orderItemQuantities.TryGetValue(orderItemId, out var purchased))
                return Result.Failure($"Order item {orderItemId} not found");
            var already = alreadyReturnedByItem.GetValueOrDefault(orderItemId, 0);
            if (qty > purchased - already)
                return Result.Failure($"Invalid return quantity for order item {orderItemId}");
        }

        return Result.Success();
    }
}
