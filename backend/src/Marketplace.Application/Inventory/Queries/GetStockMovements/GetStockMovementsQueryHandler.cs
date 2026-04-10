using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, Result<IReadOnlyList<StockMovementDto>>>
{
    private readonly IInventoryAccessService _access;
    private readonly IStockMovementRepository _movementRepository;

    public GetStockMovementsQueryHandler(IInventoryAccessService access, IStockMovementRepository movementRepository)
    {
        _access = access;
        _movementRepository = movementRepository;
    }

    public async Task<Result<IReadOnlyList<StockMovementDto>>> Handle(GetStockMovementsQuery request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.ReadInternal, ct))
                return Result<IReadOnlyList<StockMovementDto>>.Failure("Forbidden");

            var rows = await _movementRepository.ListByCompanyAndProductAsync(
                CompanyId.From(request.CompanyId),
                request.ProductId.HasValue ? ProductId.From(request.ProductId.Value) : null,
                ct);
            return Result<IReadOnlyList<StockMovementDto>>.Success(rows.Select(InventoryMapper.ToDto).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<StockMovementDto>>.Failure($"Failed to get stock movements: {ex.Message}");
        }
    }
}
