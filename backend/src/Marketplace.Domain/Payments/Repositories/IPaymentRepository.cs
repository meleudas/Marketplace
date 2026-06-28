using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Domain.Payments.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default);
    Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);
}
