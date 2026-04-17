using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderAddressSnapshotRepository : IOrderAddressSnapshotRepository
{
    private readonly ApplicationDbContext _context;

    public OrderAddressSnapshotRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<OrderAddressSnapshot>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.OrderAddresses.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddRangeAsync(IReadOnlyList<OrderAddressSnapshot> addresses, CancellationToken ct = default)
    {
        if (addresses.Count == 0)
            return;

        await _context.OrderAddresses.AddRangeAsync(addresses.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    private static OrderAddressSnapshot ToDomain(OrderAddressSnapshotRecord x) =>
        OrderAddressSnapshot.Reconstitute(
            OrderAddressId.From(x.Id),
            OrderId.From(x.OrderId),
            (OrderAddressKind)x.Kind,
            ContactPerson.Create(x.FirstName, x.LastName, x.Phone),
            Address.Create(x.Street, x.City, x.State, x.PostalCode, x.Country),
            x.CreatedAt,
            x.UpdatedAt,
            x.IsDeleted,
            x.DeletedAt);

    private static OrderAddressSnapshotRecord ToRecord(OrderAddressSnapshot x) =>
        new()
        {
            Id = x.Id.Value,
            OrderId = x.OrderId.Value,
            Kind = (short)x.Kind,
            FirstName = x.FirstName,
            LastName = x.LastName,
            Phone = x.Phone,
            Street = x.Street,
            City = x.City,
            State = x.State,
            PostalCode = x.PostalCode,
            Country = x.Country,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
