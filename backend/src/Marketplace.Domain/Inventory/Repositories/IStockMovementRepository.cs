using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Domain.Inventory.Repositories;

public interface IStockMovementRepository
{
    Task<bool> ExistsByOperationIdAsync(CompanyId companyId, string operationId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> ListByCompanyAndProductAsync(CompanyId companyId, ProductId? productId, CancellationToken ct = default);
    Task AddAsync(StockMovement movement, CancellationToken ct = default);
}
