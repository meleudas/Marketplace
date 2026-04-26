using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public OrderStatusHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderStatusHistory history, CancellationToken ct = default)
    {
        await _context.OrderStatusHistory.AddAsync(ToRecord(history), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OrderStatusHistory>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.OrderStatusHistory
            .AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    private static OrderStatusHistory ToDomain(OrderStatusHistoryRecord x) =>
        OrderStatusHistory.Reconstitute(
            OrderStatusHistoryId.From(x.Id),
            OrderId.From(x.OrderId),
            (OrderStatus)x.OldStatus,
            (OrderStatus)x.NewStatus,
            x.Comment,
            x.ChangedByUserId,
            x.Source,
            x.CorrelationId,
            x.ChangedAt,
            x.CreatedAt,
            x.UpdatedAt,
            x.IsDeleted,
            x.DeletedAt);

    private static OrderStatusHistoryRecord ToRecord(OrderStatusHistory x) => new()
    {
        Id = x.Id.Value,
        OrderId = x.OrderId.Value,
        OldStatus = (short)x.OldStatus,
        NewStatus = (short)x.NewStatus,
        Comment = x.Comment,
        ChangedByUserId = x.ChangedByUserId,
        ChangedAt = x.ChangedAt,
        Source = x.Source,
        CorrelationId = x.CorrelationId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        IsDeleted = x.IsDeleted,
        DeletedAt = x.DeletedAt
    };
}
