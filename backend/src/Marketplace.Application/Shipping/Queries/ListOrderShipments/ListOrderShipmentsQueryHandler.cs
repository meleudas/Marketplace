using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.ListOrderShipments;

public sealed record ListOrderShipmentsQuery(long OrderId, Guid ActorUserId, bool IsActorAdmin, bool IsCompanyScope, Guid? CompanyId) : IRequest<Result<IReadOnlyList<ShipmentSummaryDto>>>;

public sealed class ListOrderShipmentsQueryHandler : IRequestHandler<ListOrderShipmentsQuery, Result<IReadOnlyList<ShipmentSummaryDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipmentItemRepository _shipmentItemRepository;
    private readonly Policies.IShipmentAccessPolicy _access;

    public ListOrderShipmentsQueryHandler(
        IOrderRepository orderRepository,
        IShipmentRepository shipmentRepository,
        IShipmentItemRepository shipmentItemRepository,
        Policies.IShipmentAccessPolicy access)
    {
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
        _access = access;
    }

    public async Task<Result<IReadOnlyList<ShipmentSummaryDto>>> Handle(ListOrderShipmentsQuery request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(Domain.Common.ValueObjects.OrderId.From(request.OrderId), ct);
        if (order is null)
            return Result<IReadOnlyList<ShipmentSummaryDto>>.Failure("Order not found");
        if (request.IsCompanyScope && order.CompanyId.Value != request.CompanyId)
            return Result<IReadOnlyList<ShipmentSummaryDto>>.Failure("Order not found");

        if (!await _access.HasAccessAsync(order, request.ActorUserId, request.IsActorAdmin, Policies.ShipmentPermission.Read, ct))
            return Result<IReadOnlyList<ShipmentSummaryDto>>.Failure("Forbidden");

        var shipments = await _shipmentRepository.ListByOrderIdAsync(order.Id, ct);
        var result = new List<ShipmentSummaryDto>();
        foreach (var shipment in shipments)
        {
            var items = await _shipmentItemRepository.ListByShipmentIdAsync(shipment.Id, ct);
            result.Add(ShipmentMapper.ToSummary(shipment, items));
        }
        return Result<IReadOnlyList<ShipmentSummaryDto>>.Success(result);
    }
}
