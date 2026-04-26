using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        var row = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var rows = await _context.Orders.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default)
    {
        var q = _context.Orders.AsNoTracking().AsQueryable();

        if (filter.CustomerId.HasValue)
            q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (filter.CompanyId.HasValue)
            q = q.Where(x => x.CompanyId == filter.CompanyId.Value);
        if (filter.Statuses is { Count: > 0 })
        {
            var statuses = filter.Statuses.Select(x => (short)x).ToArray();
            q = q.Where(x => statuses.Contains(x.Status));
        }
        if (filter.CreatedFromUtc.HasValue)
            q = q.Where(x => x.CreatedAt >= filter.CreatedFromUtc.Value);
        if (filter.CreatedToUtc.HasValue)
            q = q.Where(x => x.CreatedAt <= filter.CreatedToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(x => x.OrderNumber.Contains(s));
        }

        q = (filter.Sort ?? "created_desc").Trim().ToLowerInvariant() switch
        {
            "created_asc" => q.OrderBy(x => x.CreatedAt),
            "total_asc" => q.OrderBy(x => x.TotalPrice),
            "total_desc" => q.OrderByDescending(x => x.TotalPrice),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };

        var total = await q.LongCountAsync(ct);
        var skip = Math.Max(0, (filter.Page - 1) * filter.PageSize);
        var rows = await q.Skip(skip).Take(filter.PageSize).ToListAsync(ct);
        return (rows.Select(ToDomain).ToList(), total);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        var row = ToRecord(order);
        await _context.Orders.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        var row = await _context.Orders.FirstOrDefaultAsync(x => x.Id == order.Id.Value, ct)
            ?? throw new InvalidOperationException($"Order '{order.Id.Value}' was not found.");

        row.Status = (short)order.Status;
        row.Notes = order.Notes;
        row.TrackingNumber = order.TrackingNumber;
        row.ShippedAt = order.ShippedAt;
        row.DeliveredAt = order.DeliveredAt;
        row.CancelledAt = order.CancelledAt;
        row.RefundedAt = order.RefundedAt;
        row.UpdatedAt = order.UpdatedAt;
        row.IsDeleted = order.IsDeleted;
        row.DeletedAt = order.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    private static Order ToDomain(OrderRecord x) =>
        Order.Reconstitute(
            OrderId.From(x.Id),
            x.OrderNumber,
            x.CustomerId,
            CompanyId.From(x.CompanyId),
            (OrderStatus)x.Status,
            new Money(x.TotalPrice),
            new Money(x.Subtotal),
            new Money(x.ShippingCost),
            new Money(x.DiscountAmount),
            new Money(x.TaxAmount),
            ShippingMethodId.From(x.ShippingMethodId),
            (CheckoutPaymentMethod)x.PaymentMethod,
            x.Notes,
            x.TrackingNumber,
            x.ShippedAt,
            x.DeliveredAt,
            x.CancelledAt,
            x.RefundedAt,
            x.CreatedAt,
            x.UpdatedAt,
            x.IsDeleted,
            x.DeletedAt);

    private static OrderRecord ToRecord(Order x) =>
        new()
        {
            Id = x.Id.Value,
            OrderNumber = x.OrderNumber,
            CustomerId = x.CustomerId,
            CompanyId = x.CompanyId.Value,
            Status = (short)x.Status,
            TotalPrice = x.TotalPrice.Amount,
            Subtotal = x.Subtotal.Amount,
            ShippingCost = x.ShippingCost.Amount,
            DiscountAmount = x.DiscountAmount.Amount,
            TaxAmount = x.TaxAmount.Amount,
            ShippingMethodId = x.ShippingMethodId.Value,
            PaymentMethod = (short)x.PaymentMethod,
            Notes = x.Notes,
            TrackingNumber = x.TrackingNumber,
            ShippedAt = x.ShippedAt,
            DeliveredAt = x.DeliveredAt,
            CancelledAt = x.CancelledAt,
            RefundedAt = x.RefundedAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
