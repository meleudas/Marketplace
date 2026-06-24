using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Finance.Services;

public interface IOrderFinancialsWriter
{
    Task PostOnPaymentCompletedAsync(PaymentId paymentId, CancellationToken ct = default);

    Task PostRefundReversalAsync(
        PaymentId paymentId,
        decimal refundAmount,
        string reason,
        CancellationToken ct = default);
}
