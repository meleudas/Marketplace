using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Domain.Payments.Repositories;

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(RefundId id, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> ListByStatusAsync(RefundStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<Refund> AddAsync(Refund refund, CancellationToken ct = default);
    Task UpdateAsync(Refund refund, CancellationToken ct = default);
}
