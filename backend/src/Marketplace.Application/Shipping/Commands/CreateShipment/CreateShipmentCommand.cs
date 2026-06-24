using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Policies;
using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.CreateShipment;

public sealed record CreateShipmentCommand(
    long OrderId,
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin,
    long? WarehouseId,
    IReadOnlyList<CreateShipmentLineDto> Lines,
    string? TrackingNumber) : IRequest<Result<ShipmentDetailDto>>;

public sealed record CreateShipmentLineDto(long OrderItemId, int Quantity);

public sealed class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Result<ShipmentDetailDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentAccessPolicy _access;
    private readonly IShipmentFulfillmentService _fulfillment;
    private readonly IShipmentItemRepository _shipmentItemRepository;

    public CreateShipmentCommandHandler(
        IOrderRepository orderRepository,
        IShipmentAccessPolicy access,
        IShipmentFulfillmentService fulfillment,
        IShipmentItemRepository shipmentItemRepository)
    {
        _orderRepository = orderRepository;
        _access = access;
        _fulfillment = fulfillment;
        _shipmentItemRepository = shipmentItemRepository;
    }

    public async Task<Result<ShipmentDetailDto>> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result<ShipmentDetailDto>.Failure("Order not found");
        if (order.CompanyId.Value != request.CompanyId)
            return Result<ShipmentDetailDto>.Failure("Order not found");

        if (!await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, ShipmentPermission.Manage, ct))
            return Result<ShipmentDetailDto>.Failure("Forbidden");

        var lines = request.Lines.Select(x => new CreateShipmentLineRequest(x.OrderItemId, x.Quantity)).ToList();
        var warehouseId = request.WarehouseId is null or <= 0
            ? null
            : WarehouseId.From(request.WarehouseId.Value);
        var result = await _fulfillment.CreateShipmentAsync(
            order,
            warehouseId,
            lines,
            request.TrackingNumber,
            request.ActorUserId,
            ct);
        if (!result.IsSuccess || result.Value is null)
            return Result<ShipmentDetailDto>.Failure(result.Error ?? "Failed to create shipment");

        var items = await _shipmentItemRepository.ListByShipmentIdAsync(result.Value.Id, ct);
        return Result<ShipmentDetailDto>.Success(ShipmentMapper.ToDetail(result.Value, items, []));
    }
}
