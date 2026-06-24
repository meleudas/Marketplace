using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Returns.Entities;

namespace Marketplace.Application.Returns;

internal static class ReturnMapper
{
    public static ReturnRequestDetailDto ToDetail(ReturnRequest request, IReadOnlyList<ReturnLineItem> lines) =>
        new(
            request.Id.Value,
            request.OrderId.Value,
            request.CompanyId.Value,
            request.Status.ToString(),
            request.ReasonCode.ToString(),
            request.Comment,
            request.RejectedReason,
            request.ReceivedAtUtc,
            request.RefundId,
            request.CreatedAt,
            lines.Select(x => new ReturnLineItemDto(x.OrderItemId.Value, x.Quantity, x.Reason)).ToList());

    public static ReturnRequestSummaryDto ToSummary(ReturnRequest request) =>
        new(request.Id.Value, request.OrderId.Value, request.Status.ToString(), request.ReasonCode.ToString(), request.CreatedAt);
}
