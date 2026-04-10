using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Domain.Inventory.Repositories;

public interface IInventoryReservationRepository
{
    Task<InventoryReservation?> GetByCodeAsync(CompanyId companyId, string reservationCode, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryReservation>> ListExpiredActiveAsync(DateTime utcNow, CancellationToken ct = default);
    Task AddAsync(InventoryReservation reservation, CancellationToken ct = default);
    Task UpdateAsync(InventoryReservation reservation, CancellationToken ct = default);
}
