using System.Text.Json;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.SyncPaymentStatus;

public sealed class SyncPaymentStatusCommandHandler : IRequestHandler<SyncPaymentStatusCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly IOrderCacheInvalidationService _orderCacheInvalidation;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOutboxWriter _outbox;
    private readonly IOrderStatusHistoryWriter _historyWriter;

    public SyncPaymentStatusCommandHandler(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort,
        IOrderCacheInvalidationService orderCacheInvalidation,
        IOrderPaymentStateApplier paymentStateApplier,
        IOutboxWriter outbox,
        IOrderStatusHistoryWriter historyWriter)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
        _orderCacheInvalidation = orderCacheInvalidation;
        _paymentStateApplier = paymentStateApplier;
        _outbox = outbox;
        _historyWriter = historyWriter;
    }

    public async Task<Result> Handle(SyncPaymentStatusCommand request, CancellationToken ct)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(request.PaymentId), ct);
            if (payment is null)
                return Result.Failure("Payment not found");
            if (string.IsNullOrWhiteSpace(payment.TransactionId))
                return Result.Failure("Transaction id is missing");

            var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId, ct);
            if (!statusResult.IsSuccess)
                return Result.Failure(statusResult.Error ?? "Failed to sync payment status");

            var mapped = statusResult.Status?.ToLowerInvariant() switch
            {
                "success" => PaymentTransactionStatus.Completed,
                "reversed" => PaymentTransactionStatus.Refunded,
                "refunded" => PaymentTransactionStatus.Refunded,
                "failure" => PaymentTransactionStatus.Failed,
                "error" => PaymentTransactionStatus.Failed,
                _ => PaymentTransactionStatus.Pending
            };

            payment.UpdateProviderState(mapped, statusResult.TransactionId, new JsonBlob(statusResult.RawResponse));
            await _paymentRepository.UpdateAsync(payment, ct);
            await _outbox.AppendAsync(
                "Payment",
                payment.Id.Value.ToString(),
                "PaymentStatusChanged",
                JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    paymentId = payment.Id.Value,
                    orderId = payment.OrderId.Value,
                    transactionId = statusResult.TransactionId,
                    status = mapped.ToString(),
                    source = "sync"
                }),
                ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                var oldStatus = order.Status;
                _paymentStateApplier.TryApply(order, mapped, out _);
                await _orderRepository.UpdateAsync(order, ct);
                await _historyWriter.WriteIfChangedAsync(
                    order,
                    oldStatus,
                    Guid.Empty,
                    "sync",
                    correlationId: statusResult.TransactionId,
                    ct: ct);
                await _orderCacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to sync payment: {ex.Message}");
        }
    }
}
