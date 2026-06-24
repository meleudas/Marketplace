using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Entities;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ReturnLineItemRepository : IReturnLineItemRepository
{
    private readonly ApplicationDbContext _context;

    public ReturnLineItemRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ReturnLineItem>> ListByReturnRequestIdAsync(ReturnRequestId returnRequestId, CancellationToken ct = default)
    {
        var rows = await _context.ReturnLineItems.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ReturnLineItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var returnIds = await _context.ReturnRequests.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (returnIds.Count == 0)
            return [];

        var rows = await _context.ReturnLineItems.AsNoTracking()
            .Where(x => returnIds.Contains(x.ReturnRequestId))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddRangeAsync(IReadOnlyList<ReturnLineItem> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            await _context.ReturnLineItems.AddAsync(new ReturnLineItemRecord
            {
                Id = item.Id.Value,
                ReturnRequestId = item.ReturnRequestId.Value,
                OrderItemId = item.OrderItemId.Value,
                Quantity = item.Quantity,
                Reason = item.Reason,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                IsDeleted = item.IsDeleted,
                DeletedAt = item.DeletedAt
            }, ct);
        }
        await _context.SaveChangesAsync(ct);
    }

    private static ReturnLineItem ToDomain(ReturnLineItemRecord row) =>
        ReturnLineItem.Reconstitute(
            ReturnLineItemId.From(row.Id),
            ReturnRequestId.From(row.ReturnRequestId),
            OrderItemId.From(row.OrderItemId),
            row.Quantity,
            row.Reason,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
}
