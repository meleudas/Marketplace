using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetCompanyWarehouses;

public sealed class GetCompanyWarehousesQueryHandler : IRequestHandler<GetCompanyWarehousesQuery, Result<IReadOnlyList<WarehouseDto>>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseRepository _warehouseRepository;

    public GetCompanyWarehousesQueryHandler(IInventoryAccessService access, IWarehouseRepository warehouseRepository)
    {
        _access = access;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<IReadOnlyList<WarehouseDto>>> Handle(GetCompanyWarehousesQuery request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.ReadInternal, ct))
                return Result<IReadOnlyList<WarehouseDto>>.Failure("Forbidden");

            var rows = await _warehouseRepository.ListByCompanyAsync(CompanyId.From(request.CompanyId), ct);
            return Result<IReadOnlyList<WarehouseDto>>.Success(rows.Select(InventoryMapper.ToDto).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WarehouseDto>>.Failure($"Failed to get warehouses: {ex.Message}");
        }
    }
}
