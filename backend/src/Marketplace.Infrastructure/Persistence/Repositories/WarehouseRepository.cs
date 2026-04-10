using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly ApplicationDbContext _context;

    public WarehouseRepository(ApplicationDbContext context) => _context = context;

    public async Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default)
    {
        var row = await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Warehouse>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.Warehouses
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        await _context.Warehouses.AddAsync(ToRecord(warehouse), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        var row = await _context.Warehouses.FirstOrDefaultAsync(x => x.Id == warehouse.Id.Value, ct)
            ?? throw new InvalidOperationException("Warehouse not found");
        row.Name = warehouse.Name;
        row.Code = warehouse.Code;
        row.Street = warehouse.Address.Street;
        row.City = warehouse.Address.City;
        row.State = warehouse.Address.State;
        row.PostalCode = warehouse.Address.PostalCode;
        row.Country = warehouse.Address.Country;
        row.TimeZone = warehouse.TimeZone;
        row.Priority = warehouse.Priority;
        row.IsActive = warehouse.IsActive;
        row.UpdatedAt = warehouse.UpdatedAt;
        row.IsDeleted = warehouse.IsDeleted;
        row.DeletedAt = warehouse.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static Warehouse ToDomain(WarehouseRecord row) =>
        Warehouse.Reconstitute(
            WarehouseId.From(row.Id),
            CompanyId.From(row.CompanyId),
            row.Name,
            row.Code,
            Address.Create(row.Street, row.City, row.State, row.PostalCode, row.Country),
            row.TimeZone,
            row.Priority,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static WarehouseRecord ToRecord(Warehouse x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            Name = x.Name,
            Code = x.Code,
            Street = x.Address.Street,
            City = x.Address.City,
            State = x.Address.State,
            PostalCode = x.Address.PostalCode,
            Country = x.Address.Country,
            TimeZone = x.TimeZone,
            Priority = x.Priority,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
