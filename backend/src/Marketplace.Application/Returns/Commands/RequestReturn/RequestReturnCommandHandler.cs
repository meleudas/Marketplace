using Marketplace.Application.Returns.DTOs;
using Marketplace.Application.Returns.Policies;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Returns.Entities;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Commands.RequestReturn;

public sealed record RequestReturnCommand(
    long OrderId,
    Guid ActorUserId,
    ReturnReasonCode ReasonCode,
    string? Comment,
    IReadOnlyList<RequestReturnLineDto> Lines) : IRequest<Result<ReturnRequestDetailDto>>;

public sealed record RequestReturnLineDto(long OrderItemId, int Quantity, string? Reason);

public sealed class RequestReturnCommandHandler : IRequestHandler<RequestReturnCommand, Result<ReturnRequestDetailDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IReturnLineItemRepository _returnLineItemRepository;
    private readonly ReturnRequestPolicy _policy;

    public RequestReturnCommandHandler(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IReturnRequestRepository returnRepository,
        IReturnLineItemRepository returnLineItemRepository,
        ReturnRequestPolicy policy)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _returnRepository = returnRepository;
        _returnLineItemRepository = returnLineItemRepository;
        _policy = policy;
    }

    public async Task<Result<ReturnRequestDetailDto>> Handle(RequestReturnCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result<ReturnRequestDetailDto>.Failure("Order not found");
        if (order.CustomerId != request.ActorUserId)
            return Result<ReturnRequestDetailDto>.Failure("Forbidden");

        var orderItems = await _orderItemRepository.ListByOrderIdAsync(order.Id, ct);
        var existingLines = await _returnLineItemRepository.ListByOrderIdAsync(order.Id, ct);
        var alreadyReturned = existingLines
            .GroupBy(x => x.OrderItemId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var requested = request.Lines.Select(x => (x.OrderItemId, x.Quantity)).ToList();
        var orderItemQuantities = orderItems.ToDictionary(x => x.Id.Value, x => x.Quantity);
        var policyResult = _policy.ValidateRequest(
            order, request.ReasonCode, request.Comment, DateTime.UtcNow, alreadyReturned, orderItemQuantities, requested);
        if (!policyResult.IsSuccess)
            return Result<ReturnRequestDetailDto>.Failure(policyResult.Error ?? "Validation failed");

        foreach (var line in request.Lines)
        {
            var orderItem = orderItems.FirstOrDefault(x => x.Id.Value == line.OrderItemId);
            if (orderItem is null)
                return Result<ReturnRequestDetailDto>.Failure($"Order item {line.OrderItemId} not found");
            var already = alreadyReturned.GetValueOrDefault(line.OrderItemId, 0);
            if (line.Quantity > orderItem.Quantity - already)
                return Result<ReturnRequestDetailDto>.Failure($"Invalid return quantity for item {line.OrderItemId}");
        }

        var returnRequest = ReturnRequest.Create(
            ReturnRequestId.From(0),
            order.Id,
            order.CustomerId,
            order.CompanyId,
            request.ReasonCode,
            request.Comment);
        var saved = await _returnRepository.AddAsync(returnRequest, ct);

        var lineEntities = request.Lines.Select(line =>
            ReturnLineItem.Create(
                ReturnLineItemId.From(0),
                saved.Id,
                OrderItemId.From(line.OrderItemId),
                line.Quantity,
                line.Reason)).ToList();
        await _returnLineItemRepository.AddRangeAsync(lineEntities, ct);

        return Result<ReturnRequestDetailDto>.Success(ReturnMapper.ToDetail(saved, lineEntities));
    }
}
