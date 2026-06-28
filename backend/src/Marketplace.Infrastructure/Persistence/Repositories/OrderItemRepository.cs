using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderItemRepository : IOrderItemRepository
{
    private readonly ApplicationDbContext _context;

    public OrderItemRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<OrderItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.OrderItems.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddRangeAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0)
            return;
        await _context.OrderItems.AddRangeAsync(items.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    private static OrderItem ToDomain(OrderItemRecord x) =>
        OrderItem.Reconstitute(
            OrderItemId.From(x.Id),
            OrderId.From(x.OrderId),
            ProductId.From(x.ProductId),
            x.ProductName,
            x.ProductImage,
            x.Quantity,
            new Money(x.PriceAtMoment),
            new Money(x.Discount),
            new Money(x.TotalPrice),
            CompanyId.From(x.CompanyId),
            x.CreatedAt,
            x.UpdatedAt,
            x.IsDeleted,
            x.DeletedAt);

    private static OrderItemRecord ToRecord(OrderItem x) =>
        new()
        {
            Id = x.Id.Value,
            OrderId = x.OrderId.Value,
            ProductId = x.ProductId.Value,
            ProductName = x.ProductName,
            ProductImage = x.ProductImage,
            Quantity = x.Quantity,
            PriceAtMoment = x.PriceAtMoment.Amount,
            Discount = x.Discount.Amount,
            TotalPrice = x.TotalPrice.Amount,
            CompanyId = x.CompanyId.Value,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
