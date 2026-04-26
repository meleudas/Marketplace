using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Application.Orders.DTOs;

public sealed record PagedOrdersDto(IReadOnlyList<OrderListItemDto> Items, long Total, int Page, int PageSize);

public sealed record OrderListItemDto(
    long OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid CompanyId,
    OrderStatus Status,
    decimal TotalPrice,
    string PaymentMethod,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderDetailsDto(
    long OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid CompanyId,
    OrderStatus Status,
    decimal TotalPrice,
    decimal Subtotal,
    decimal ShippingCost,
    decimal DiscountAmount,
    decimal TaxAmount,
    string PaymentMethod,
    string? Notes,
    string? TrackingNumber,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderAddressDto> Addresses,
    PaymentSnapshotDto? Payment,
    IReadOnlyList<RefundSnapshotDto> Refunds,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory);

public sealed record OrderItemDto(
    long ProductId,
    string ProductName,
    string? ProductImage,
    int Quantity,
    decimal PriceAtMoment,
    decimal Discount,
    decimal TotalPrice);

public sealed record OrderAddressDto(
    string Kind,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record PaymentSnapshotDto(
    long PaymentId,
    string Method,
    decimal Amount,
    string Currency,
    string? TransactionId,
    string Status,
    DateTime? ProcessedAt);

public sealed record RefundSnapshotDto(
    long RefundId,
    decimal Amount,
    string Reason,
    string Status,
    Guid? ProcessedByUserId,
    DateTime? ProcessedAt,
    DateTime CreatedAt);

public sealed record OrderStatusHistoryDto(
    string OldStatus,
    string NewStatus,
    Guid ChangedByUserId,
    string Source,
    string? Comment,
    string? CorrelationId,
    DateTime ChangedAt);
