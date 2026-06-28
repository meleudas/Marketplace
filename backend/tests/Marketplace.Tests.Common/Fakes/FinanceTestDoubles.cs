
namespace Marketplace.Tests.Common.Fakes;

using Marketplace.Application.Finance.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Entities;

public sealed class NoopOrderFinancialsWriter : IOrderFinancialsWriter
{
    public Task PostOnPaymentCompletedAsync(PaymentId paymentId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task PostRefundReversalAsync(
        PaymentId paymentId,
        decimal refundAmount,
        string reason,
        CancellationToken ct = default) =>
        Task.CompletedTask;
}
