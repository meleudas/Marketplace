using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;

namespace Marketplace.Domain.Finance.Repositories;

public interface IOrderFinancialsRepository
{
    Task<OrderFinancials?> GetByIdAsync(OrderFinancialsId id, CancellationToken ct = default);
    Task<OrderFinancials?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<OrderFinancials?> GetByPaymentIdAsync(PaymentId paymentId, CancellationToken ct = default);
    Task<OrderFinancials> AddAsync(OrderFinancials financials, CancellationToken ct = default);
}
