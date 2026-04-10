using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Domain.Inventory.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default);
    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
}
