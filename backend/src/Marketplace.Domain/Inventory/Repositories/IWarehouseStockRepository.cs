using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Domain.Inventory.Repositories;

public interface IWarehouseStockRepository
{
    Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default);
    Task AddAsync(WarehouseStock stock, CancellationToken ct = default);
    Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default);
}
