using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.ListMyShipments;

public sealed class ListMyShipmentsQueryHandler : IRequestHandler<ListMyShipmentsQuery, Result<IReadOnlyList<ShipmentDto>>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public ListMyShipmentsQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<Result<IReadOnlyList<ShipmentDto>>> Handle(ListMyShipmentsQuery request, CancellationToken ct)
    {
        try
        {
            var rows = await _shipmentRepository.ListByCustomerAsync(request.ActorUserId, ct);
            return Result<IReadOnlyList<ShipmentDto>>.Success(rows.Select(x => x.ToDto()).ToList());
        }
        catch
        {
            return Result<IReadOnlyList<ShipmentDto>>.Failure("Failed to list shipments");
        }
    }
}
