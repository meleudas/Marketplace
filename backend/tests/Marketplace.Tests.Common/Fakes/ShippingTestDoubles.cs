using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Tests.Common.Fakes;

public sealed class NoopShipmentFulfillmentService : IShipmentFulfillmentService
{
    public Task<Result<Shipment>> CreateShipmentAsync(
        Order order,
        WarehouseId? warehouseId,
        IReadOnlyList<CreateShipmentLineRequest> lines,
        string? trackingNumber,
        Guid actorUserId,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Shipment>.Failure("not used"));

    public Task<Result> ApplyCarrierEventAsync(
        ShippingCarrierCode carrier,
        string eventKey,
        string payloadHash,
        string rawPayload,
        CancellationToken ct = default) =>
        Task.FromResult(Result.Success());

    public Task<ShipmentFulfillmentSummary> BuildSummaryAsync(OrderId orderId, CancellationToken ct = default) =>
        Task.FromResult(new ShipmentFulfillmentSummary(0, 0, 0, false, false));

    public Task<FulfillmentReadinessDto> BuildReadinessDtoAsync(OrderId orderId, CancellationToken ct = default) =>
        Task.FromResult(new FulfillmentReadinessDto(1, 1, 0, true, false, [], [], []));
}
