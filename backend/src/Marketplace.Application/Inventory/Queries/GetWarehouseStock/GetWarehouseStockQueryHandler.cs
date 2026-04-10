using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetWarehouseStock;

public sealed class GetWarehouseStockQueryHandler : IRequestHandler<GetWarehouseStockQuery, Result<IReadOnlyList<WarehouseStockDto>>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseStockRepository _stockRepository;

    public GetWarehouseStockQueryHandler(IInventoryAccessService access, IWarehouseStockRepository stockRepository)
    {
        _access = access;
        _stockRepository = stockRepository;
    }

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> Handle(GetWarehouseStockQuery request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.ReadInternal, ct))
                return Result<IReadOnlyList<WarehouseStockDto>>.Failure("Forbidden");

            var all = await _stockRepository.ListByCompanyAsync(CompanyId.From(request.CompanyId), ct);
            var filtered = all.Where(x =>
                (!request.WarehouseId.HasValue || x.WarehouseId.Value == request.WarehouseId.Value) &&
                (!request.ProductId.HasValue || x.ProductId.Value == request.ProductId.Value)).ToList();

            return Result<IReadOnlyList<WarehouseStockDto>>.Success(filtered.Select(InventoryMapper.ToDto).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WarehouseStockDto>>.Failure($"Failed to get stock: {ex.Message}");
        }
    }
}
