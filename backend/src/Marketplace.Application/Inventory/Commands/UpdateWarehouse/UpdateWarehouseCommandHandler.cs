using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, Result<WarehouseDto>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseRepository _warehouseRepository;

    public UpdateWarehouseCommandHandler(IInventoryAccessService access, IWarehouseRepository warehouseRepository)
    {
        _access = access;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<WarehouseDto>> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result<WarehouseDto>.Failure("Forbidden");

            var warehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.WarehouseId), ct);
            if (warehouse is null || warehouse.CompanyId.Value != request.CompanyId)
                return Result<WarehouseDto>.Failure("Warehouse not found");

            warehouse.Update(
                request.Name,
                request.Code,
                Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country),
                request.TimeZone,
                request.Priority);

            await _warehouseRepository.UpdateAsync(warehouse, ct);
            return Result<WarehouseDto>.Success(InventoryMapper.ToDto(warehouse));
        }
        catch (Exception ex)
        {
            return Result<WarehouseDto>.Failure($"Failed to update warehouse: {ex.Message}");
        }
    }
}
