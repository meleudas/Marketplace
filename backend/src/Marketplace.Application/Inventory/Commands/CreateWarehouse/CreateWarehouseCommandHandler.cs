using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Result<WarehouseDto>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseRepository _warehouseRepository;

    public CreateWarehouseCommandHandler(IInventoryAccessService access, IWarehouseRepository warehouseRepository)
    {
        _access = access;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<WarehouseDto>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result<WarehouseDto>.Failure("Forbidden");

            var warehouse = Warehouse.Create(
                WarehouseId.From(0),
                CompanyId.From(request.CompanyId),
                request.Name,
                request.Code,
                Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country),
                request.TimeZone,
                request.Priority);

            await _warehouseRepository.AddAsync(warehouse, ct);
            return Result<WarehouseDto>.Success(InventoryMapper.ToDto(warehouse));
        }
        catch (Exception ex)
        {
            return Result<WarehouseDto>.Failure($"Failed to create warehouse: {ex.Message}");
        }
    }
}
