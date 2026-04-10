using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly ApplicationDbContext _context;

    public WarehouseStockRepository(ApplicationDbContext context) => _context = context;

    public async Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default)
    {
        var row = await _context.WarehouseStocks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId.Value && x.ProductId == productId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.WarehouseStocks.AsNoTracking().Where(x => x.CompanyId == companyId.Value).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
    {
        var rows = await _context.WarehouseStocks.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.ProductId == productId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(WarehouseStock stock, CancellationToken ct = default)
    {
        await _context.WarehouseStocks.AddAsync(ToRecord(stock), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default)
    {
        var row = await _context.WarehouseStocks
            .FirstOrDefaultAsync(x => x.Id == stock.Id.Value, ct)
            ?? throw new InvalidOperationException("Warehouse stock not found");
        row.OnHand = stock.OnHand;
        row.Reserved = stock.Reserved;
        row.ReorderPoint = stock.ReorderPoint;
        row.Version = stock.Version;
        row.UpdatedAt = stock.UpdatedAt;
        row.IsDeleted = stock.IsDeleted;
        row.DeletedAt = stock.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static WarehouseStock ToDomain(WarehouseStockRecord row) =>
        WarehouseStock.Reconstitute(
            WarehouseStockId.From(row.Id),
            CompanyId.From(row.CompanyId),
            WarehouseId.From(row.WarehouseId),
            ProductId.From(row.ProductId),
            row.OnHand,
            row.Reserved,
            row.ReorderPoint,
            row.Version,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static WarehouseStockRecord ToRecord(WarehouseStock row) =>
        new()
        {
            Id = row.Id.Value,
            CompanyId = row.CompanyId.Value,
            WarehouseId = row.WarehouseId.Value,
            ProductId = row.ProductId.Value,
            OnHand = row.OnHand,
            Reserved = row.Reserved,
            ReorderPoint = row.ReorderPoint,
            Version = row.Version,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            IsDeleted = row.IsDeleted,
            DeletedAt = row.DeletedAt
        };
}
