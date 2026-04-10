using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class InventoryReservationRepository : IInventoryReservationRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryReservationRepository(ApplicationDbContext context) => _context = context;

    public async Task<InventoryReservation?> GetByCodeAsync(CompanyId companyId, string reservationCode, CancellationToken ct = default)
    {
        var row = await _context.InventoryReservations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.ReservationCode == reservationCode, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<InventoryReservation>> ListExpiredActiveAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var rows = await _context.InventoryReservations.AsNoTracking()
            .Where(x => x.Status == (short)InventoryReservationStatus.Active && x.ExpiresAt <= utcNow)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(InventoryReservation reservation, CancellationToken ct = default)
    {
        await _context.InventoryReservations.AddAsync(ToRecord(reservation), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InventoryReservation reservation, CancellationToken ct = default)
    {
        var row = await _context.InventoryReservations.FirstOrDefaultAsync(x => x.Id == reservation.Id.Value, ct)
            ?? throw new InvalidOperationException("Reservation not found");
        row.Status = (short)reservation.Status;
        row.UpdatedAt = reservation.UpdatedAt;
        row.IsDeleted = reservation.IsDeleted;
        row.DeletedAt = reservation.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static InventoryReservation ToDomain(InventoryReservationRecord row) =>
        InventoryReservation.Reconstitute(
            InventoryReservationId.From(row.Id),
            CompanyId.From(row.CompanyId),
            WarehouseId.From(row.WarehouseId),
            ProductId.From(row.ProductId),
            row.ReservationCode,
            row.Quantity,
            (InventoryReservationStatus)row.Status,
            row.ExpiresAt,
            row.Reference,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static InventoryReservationRecord ToRecord(InventoryReservation x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            WarehouseId = x.WarehouseId.Value,
            ProductId = x.ProductId.Value,
            ReservationCode = x.ReservationCode,
            Quantity = x.Quantity,
            Status = (short)x.Status,
            ExpiresAt = x.ExpiresAt,
            Reference = x.Reference,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
