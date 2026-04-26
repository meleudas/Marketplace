using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailsDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderAddressSnapshotRepository _orderAddressRepository;
    private readonly IOrderStatusHistoryRepository _orderStatusHistoryRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IOrderAccessService _access;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IOrderAddressSnapshotRepository orderAddressRepository,
        IOrderStatusHistoryRepository orderStatusHistoryRepository,
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IOrderAccessService access,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _orderAddressRepository = orderAddressRepository;
        _orderStatusHistoryRepository = orderStatusHistoryRepository;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _access = access;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var key = OrderCacheKeys.Detail(request.OrderId);
        var cached = await _cache.GetAsync<OrderDetailsDto>(key, ct);
        if (cached is not null)
        {
            // Access still checked against fresh order to avoid leaking cached data cross-user.
            var ord = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
            if (ord is null)
                return Result<OrderDetailsDto>.Failure("Order not found");
            var canReadCached = await _access.HasAccessAsync(ord, request.ActorUserId, request.IsActorAdmin, OrderPermission.Read, ct);
            return canReadCached ? Result<OrderDetailsDto>.Success(cached) : Result<OrderDetailsDto>.Failure("Forbidden");
        }

        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result<OrderDetailsDto>.Failure("Order not found");

        var canRead = await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, OrderPermission.Read, ct);
        if (!canRead)
            return Result<OrderDetailsDto>.Failure("Forbidden");

        var items = await _orderItemRepository.ListByOrderIdAsync(order.Id, ct);
        var addresses = await _orderAddressRepository.ListByOrderIdAsync(order.Id, ct);
        var payment = await _paymentRepository.GetByOrderIdAsync(order.Id, ct);
        var refunds = await _refundRepository.ListByOrderIdAsync(order.Id, ct);
        var statusHistory = await _orderStatusHistoryRepository.ListByOrderIdAsync(order.Id, ct);

        var dto = new OrderDetailsDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerId,
            order.CompanyId.Value,
            order.Status,
            order.TotalPrice.Amount,
            order.Subtotal.Amount,
            order.ShippingCost.Amount,
            order.DiscountAmount.Amount,
            order.TaxAmount.Amount,
            order.PaymentMethod.ToString(),
            order.Notes,
            order.TrackingNumber,
            order.ShippedAt,
            order.DeliveredAt,
            order.CancelledAt,
            order.RefundedAt,
            order.CreatedAt,
            order.UpdatedAt,
            items.Select(x => new OrderItemDto(
                x.ProductId.Value,
                x.ProductName,
                x.ProductImage,
                x.Quantity,
                x.PriceAtMoment.Amount,
                x.Discount.Amount,
                x.TotalPrice.Amount)).ToList(),
            addresses.Select(x => new OrderAddressDto(
                x.Kind.ToString(),
                x.FirstName,
                x.LastName,
                x.Phone,
                x.Street,
                x.City,
                x.State,
                x.PostalCode,
                x.Country)).ToList(),
            payment is null ? null : new PaymentSnapshotDto(
                payment.Id.Value,
                payment.PaymentMethod.ToString(),
                payment.Amount.Amount,
                payment.Currency,
                payment.TransactionId,
                payment.Status.ToString(),
                payment.ProcessedAt),
            refunds.Select(x => new RefundSnapshotDto(
                x.Id.Value,
                x.Amount.Amount,
                x.Reason,
                x.Status.ToString(),
                x.ProcessedByUserId,
                x.ProcessedAt,
                x.CreatedAt)).ToList(),
            statusHistory.Select(x => new OrderStatusHistoryDto(
                x.OldStatus.ToString(),
                x.NewStatus.ToString(),
                x.ChangedByUserId,
                x.Source,
                x.Comment,
                x.CorrelationId,
                x.ChangedAt)).ToList());

        await _cache.SetAsync(key, dto, _ttl.OrderDetail, ct);
        return Result<OrderDetailsDto>.Success(dto);
    }
}
