using Marketplace.Application.Inventory.Authorization;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.DeactivateWarehouse;

public sealed class DeactivateWarehouseCommandHandler : IRequestHandler<DeactivateWarehouseCommand, Result>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseRepository _warehouseRepository;

    public DeactivateWarehouseCommandHandler(IInventoryAccessService access, IWarehouseRepository warehouseRepository)
    {
        _access = access;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result> Handle(DeactivateWarehouseCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result.Failure("Forbidden");

            var warehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.WarehouseId), ct);
            if (warehouse is null || warehouse.CompanyId.Value != request.CompanyId)
                return Result.Failure("Warehouse not found");

            warehouse.Deactivate();
            await _warehouseRepository.UpdateAsync(warehouse, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to deactivate warehouse: {ex.Message}");
        }
    }
}
