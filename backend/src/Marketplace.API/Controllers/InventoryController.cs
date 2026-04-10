using Marketplace.API.Extensions;
using Marketplace.Application.Inventory.Commands.AdjustStock;
using Marketplace.Application.Inventory.Commands.CreateWarehouse;
using Marketplace.Application.Inventory.Commands.DeactivateWarehouse;
using Marketplace.Application.Inventory.Commands.ReceiveStock;
using Marketplace.Application.Inventory.Commands.ReleaseReservation;
using Marketplace.Application.Inventory.Commands.ReserveStock;
using Marketplace.Application.Inventory.Commands.ShipStock;
using Marketplace.Application.Inventory.Commands.TransferStock;
using Marketplace.Application.Inventory.Commands.UpdateWarehouse;
using Marketplace.Application.Inventory.Queries.GetCompanyWarehouses;
using Marketplace.Application.Inventory.Queries.GetWarehouseStock;
using Marketplace.Application.Inventory.Queries.GetStockMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}")]
[Authorize]
public sealed class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(Guid companyId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetCompanyWarehousesQuery(companyId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse(Guid companyId, [FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new CreateWarehouseCommand(
            companyId, actorId, User.IsInRole("Admin"),
            request.Name, request.Code, request.Street, request.City, request.State, request.PostalCode, request.Country,
            request.TimeZone, request.Priority), ct);
        return result.ToActionResult();
    }

    [HttpPut("warehouses/{warehouseId:long}")]
    public async Task<IActionResult> UpdateWarehouse(Guid companyId, long warehouseId, [FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpdateWarehouseCommand(
            companyId, warehouseId, actorId, User.IsInRole("Admin"),
            request.Name, request.Code, request.Street, request.City, request.State, request.PostalCode, request.Country,
            request.TimeZone, request.Priority), ct);
        return result.ToActionResult();
    }

    [HttpPost("warehouses/{warehouseId:long}/deactivate")]
    public async Task<IActionResult> DeactivateWarehouse(Guid companyId, long warehouseId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new DeactivateWarehouseCommand(companyId, warehouseId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpGet("inventory/stocks")]
    public async Task<IActionResult> GetStock(Guid companyId, [FromQuery] long? warehouseId, [FromQuery] long? productId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetWarehouseStockQuery(companyId, warehouseId, productId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpGet("inventory/movements")]
    public async Task<IActionResult> GetMovements(Guid companyId, [FromQuery] long? productId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetStockMovementsQuery(companyId, productId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("inventory/receive")]
    public async Task<IActionResult> Receive(Guid companyId, [FromBody] StockOperationRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ReceiveStockCommand(
            companyId, request.WarehouseId, request.ProductId, request.Quantity, request.OperationId,
            request.Reference, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("inventory/ship")]
    public async Task<IActionResult> Ship(Guid companyId, [FromBody] StockOperationRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ShipStockCommand(
            companyId, request.WarehouseId, request.ProductId, request.Quantity, request.OperationId,
            request.Reference, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("inventory/adjust")]
    public async Task<IActionResult> Adjust(Guid companyId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new AdjustStockCommand(
            companyId, request.WarehouseId, request.ProductId, request.OnHand, request.Reserved, request.ReorderPoint,
            request.OperationId, request.Reason, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("inventory/transfer")]
    public async Task<IActionResult> Transfer(Guid companyId, [FromBody] TransferStockRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new TransferStockCommand(
            companyId, request.FromWarehouseId, request.ToWarehouseId, request.ProductId, request.Quantity,
            request.OperationId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost("inventory/reservations")]
    public async Task<IActionResult> Reserve(Guid companyId, [FromBody] ReserveStockRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ReserveStockCommand(
            companyId, request.WarehouseId, request.ProductId, request.Quantity, request.ReservationCode,
            request.TtlMinutes, request.Reference, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpDelete("inventory/reservations/{reservationCode}")]
    public async Task<IActionResult> Release(Guid companyId, string reservationCode, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ReleaseReservationCommand(companyId, reservationCode, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }
}

public sealed record CreateWarehouseRequest(
    string Name,
    string Code,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string TimeZone,
    int Priority);

public sealed record StockOperationRequest(
    long WarehouseId,
    long ProductId,
    int Quantity,
    string OperationId,
    string? Reference);

public sealed record AdjustStockRequest(
    long WarehouseId,
    long ProductId,
    int OnHand,
    int Reserved,
    int ReorderPoint,
    string OperationId,
    string Reason);

public sealed record TransferStockRequest(
    long FromWarehouseId,
    long ToWarehouseId,
    long ProductId,
    int Quantity,
    string OperationId);

public sealed record ReserveStockRequest(
    long WarehouseId,
    long ProductId,
    int Quantity,
    string ReservationCode,
    int TtlMinutes,
    string? Reference);
