using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly ApplicationDbContext _context;

    public StockMovementRepository(ApplicationDbContext context) => _context = context;

    public Task<bool> ExistsByOperationIdAsync(CompanyId companyId, string operationId, CancellationToken ct = default)
        => _context.StockMovements.AnyAsync(x => x.CompanyId == companyId.Value && x.OperationId == operationId, ct);

    public async Task<IReadOnlyList<StockMovement>> ListByCompanyAndProductAsync(CompanyId companyId, ProductId? productId, CancellationToken ct = default)
    {
        var query = _context.StockMovements.AsNoTracking().Where(x => x.CompanyId == companyId.Value);
        if (productId is not null)
            query = query.Where(x => x.ProductId == productId.Value);

        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(StockMovement movement, CancellationToken ct = default)
    {
        await _context.StockMovements.AddAsync(ToRecord(movement), ct);
        await _context.SaveChangesAsync(ct);
    }

    private static StockMovement ToDomain(StockMovementRecord row) =>
        StockMovement.Reconstitute(
            StockMovementId.From(row.Id),
            CompanyId.From(row.CompanyId),
            WarehouseId.From(row.WarehouseId),
            ProductId.From(row.ProductId),
            (StockMovementType)row.Type,
            row.Quantity,
            row.OperationId,
            row.Reference,
            row.Reason,
            row.ActorUserId,
            row.OccurredAt,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static StockMovementRecord ToRecord(StockMovement row) =>
        new()
        {
            Id = row.Id.Value,
            CompanyId = row.CompanyId.Value,
            WarehouseId = row.WarehouseId.Value,
            ProductId = row.ProductId.Value,
            Type = (short)row.Type,
            Quantity = row.Quantity,
            OperationId = row.OperationId,
            Reference = row.Reference,
            Reason = row.Reason,
            ActorUserId = row.ActorUserId,
            OccurredAt = row.OccurredAt,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            IsDeleted = row.IsDeleted,
            DeletedAt = row.DeletedAt
        };
}
