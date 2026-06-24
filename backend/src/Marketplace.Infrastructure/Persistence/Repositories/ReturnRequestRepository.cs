using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Entities;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ReturnRequestRepository(ApplicationDbContext context) => _context = context;

    public async Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken ct = default)
    {
        var row = await _context.ReturnRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ReturnRequest>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var rows = await _context.ReturnRequests.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ReturnRequest>> ListByCompanyAsync(CompanyId companyId, ReturnRequestStatus? status, CancellationToken ct = default)
    {
        var q = _context.ReturnRequests.AsNoTracking().Where(x => x.CompanyId == companyId.Value);
        if (status.HasValue)
            q = q.Where(x => x.Status == (short)status.Value);
        var rows = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ReturnRequest>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.ReturnRequests.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<ReturnRequest> AddAsync(ReturnRequest entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.ReturnRequests.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(ReturnRequest entity, CancellationToken ct = default)
    {
        var row = await _context.ReturnRequests.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Return request '{entity.Id.Value}' was not found.");
        row.Status = (short)entity.Status;
        row.ApprovedByUserId = entity.ApprovedByUserId;
        row.RejectedReason = entity.RejectedReason;
        row.ReceivedAtUtc = entity.ReceivedAtUtc;
        row.RefundId = entity.RefundId;
        row.UpdatedAt = entity.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static ReturnRequest ToDomain(ReturnRequestRecord row) =>
        ReturnRequest.Reconstitute(
            ReturnRequestId.From(row.Id),
            OrderId.From(row.OrderId),
            row.CustomerId,
            CompanyId.From(row.CompanyId),
            (ReturnRequestStatus)row.Status,
            (ReturnReasonCode)row.ReasonCode,
            row.Comment,
            row.ApprovedByUserId,
            row.RejectedReason,
            row.ReceivedAtUtc,
            row.RefundId,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ReturnRequestRecord ToRecord(ReturnRequest entity) =>
        new()
        {
            Id = entity.Id.Value,
            OrderId = entity.OrderId.Value,
            CustomerId = entity.CustomerId,
            CompanyId = entity.CompanyId.Value,
            Status = (short)entity.Status,
            ReasonCode = (short)entity.ReasonCode,
            Comment = entity.Comment,
            ApprovedByUserId = entity.ApprovedByUserId,
            RejectedReason = entity.RejectedReason,
            ReceivedAtUtc = entity.ReceivedAtUtc,
            RefundId = entity.RefundId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
