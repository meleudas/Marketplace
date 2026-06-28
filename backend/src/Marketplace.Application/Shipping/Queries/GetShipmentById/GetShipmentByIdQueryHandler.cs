using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Policies;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.GetShipmentById;

public sealed record GetShipmentByIdQuery(long ShipmentId, Guid ActorUserId, bool IsActorAdmin) : IRequest<Result<ShipmentDetailDto>>;

public sealed class GetShipmentByIdQueryHandler : IRequestHandler<GetShipmentByIdQuery, Result<ShipmentDetailDto>>
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipmentItemRepository _shipmentItemRepository;
    private readonly IShippingEventRepository _shippingEventRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentAccessPolicy _access;

    public GetShipmentByIdQueryHandler(
        IShipmentRepository shipmentRepository,
        IShipmentItemRepository shipmentItemRepository,
        IShippingEventRepository shippingEventRepository,
        IOrderRepository orderRepository,
        IShipmentAccessPolicy access)
    {
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
        _shippingEventRepository = shippingEventRepository;
        _orderRepository = orderRepository;
        _access = access;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(GetShipmentByIdQuery request, CancellationToken ct)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(ShipmentId.From(request.ShipmentId), ct);
        if (shipment is null)
            return Result<ShipmentDetailDto>.Failure("Shipment not found");

        var order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct);
        if (order is null)
            return Result<ShipmentDetailDto>.Failure("Order not found");

        if (!await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, ShipmentPermission.Read, ct))
            return Result<ShipmentDetailDto>.Failure("Forbidden");

        var items = await _shipmentItemRepository.ListByShipmentIdAsync(shipment.Id, ct);
        var events = await _shippingEventRepository.ListByShipmentIdAsync(shipment.Id, ct);
        var eventDtos = events.Select(x => new ShippingEventDto(
            x.CarrierCode.ToString(),
            x.EventKey,
            x.TrackingNumber,
            x.DeliveryStatus?.ToString(),
            x.OccurredAtUtc ?? x.ReceivedAtUtc)).ToList();

        return Result<ShipmentDetailDto>.Success(ShipmentMapper.ToDetail(shipment, items, eventDtos));
    }
}
