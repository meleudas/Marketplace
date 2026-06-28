namespace Marketplace.Application.Returns.DTOs;

public sealed record ReturnLineItemDto(long OrderItemId, int Quantity, string? Reason);

public sealed record ReturnRequestSummaryDto(
    long ReturnId,
    long OrderId,
    string Status,
    string ReasonCode,
    DateTime CreatedAt);

public sealed record ReturnRequestDetailDto(
    long ReturnId,
    long OrderId,
    Guid CompanyId,
    string Status,
    string ReasonCode,
    string? Comment,
    string? RejectedReason,
    DateTime? ReceivedAtUtc,
    long? RefundId,
    DateTime CreatedAt,
    IReadOnlyList<ReturnLineItemDto> Lines);
