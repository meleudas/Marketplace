using System.Text;
using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Application.Payments.Commands.RequestRefund;
using Marketplace.Application.Payments.Commands.SyncPaymentStatus;
using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;

namespace Marketplace.Tests;

public class ApplicationPaymentFlowTests
{
    [Fact]
    public async Task Webhook_Handler_Updates_Payment_And_Order_Status()
    {
        var paymentRepo = new InMemoryPaymentRepository();
        var orderRepo = new InMemoryOrderRepository();
        var order = await orderRepo.AddAsync(Order.Reconstitute(
            OrderId.From(0), "ORD-1", Guid.NewGuid(), CompanyId.From(Guid.NewGuid()), OrderStatus.Pending,
            new Money(100), new Money(100), Money.Zero, Money.Zero, Money.Zero, ShippingMethodId.From(0), CheckoutPaymentMethod.Card,
            null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null));
        _ = await paymentRepo.AddAsync(Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(100), "UAH", "ORD-1", PaymentTransactionStatus.Pending, JsonBlob.Empty));

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"order_id\":\"ORD-1\",\"status\":\"success\"}"));
        var handler = new HandleLiqPayWebhookCommandHandler(new FakeLiqPayPort(), paymentRepo, orderRepo);
        var result = await handler.Handle(new HandleLiqPayWebhookCommand(payload, "sig"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentTransactionStatus.Completed, (await paymentRepo.GetByTransactionIdAsync("ORD-1"))!.Status);
        Assert.Equal(OrderStatus.Paid, (await orderRepo.GetByIdAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task Refund_Command_Refunds_Completed_Payment()
    {
        var paymentRepo = new InMemoryPaymentRepository();
        var orderRepo = new InMemoryOrderRepository();
        var refundRepo = new InMemoryRefundRepository();
        var order = await orderRepo.AddAsync(Order.Reconstitute(
            OrderId.From(0), "ORD-2", Guid.NewGuid(), CompanyId.From(Guid.NewGuid()), OrderStatus.Paid,
            new Money(200), new Money(200), Money.Zero, Money.Zero, Money.Zero, ShippingMethodId.From(0), CheckoutPaymentMethod.Card,
            null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null));
        var payment = await paymentRepo.AddAsync(Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(200), "UAH", "ORD-2", PaymentTransactionStatus.Completed, JsonBlob.Empty));

        var handler = new RequestRefundCommandHandler(paymentRepo, refundRepo, orderRepo, new FakeLiqPayPort());
        var result = await handler.Handle(new RequestRefundCommand(payment.Id.Value, 100m, "Customer request", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(refundRepo.Items);
        Assert.Equal(OrderStatus.Refunded, (await orderRepo.GetByIdAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task Sync_Command_Updates_Status_From_Provider()
    {
        var paymentRepo = new InMemoryPaymentRepository();
        var orderRepo = new InMemoryOrderRepository();
        var order = await orderRepo.AddAsync(Order.Reconstitute(
            OrderId.From(0), "ORD-3", Guid.NewGuid(), CompanyId.From(Guid.NewGuid()), OrderStatus.Pending,
            new Money(50), new Money(50), Money.Zero, Money.Zero, Money.Zero, ShippingMethodId.From(0), CheckoutPaymentMethod.Card,
            null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null));
        var payment = await paymentRepo.AddAsync(Payment.Create(PaymentId.From(0), order.Id, PaymentMethodKind.LiqPay, new Money(50), "UAH", "ORD-3", PaymentTransactionStatus.Pending, JsonBlob.Empty));

        var handler = new SyncPaymentStatusCommandHandler(paymentRepo, orderRepo, new FakeLiqPayPort());
        var result = await handler.Handle(new SyncPaymentStatusCommand(payment.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentTransactionStatus.Completed, (await paymentRepo.GetByIdAsync(payment.Id))!.Status);
        Assert.Equal(OrderStatus.Paid, (await orderRepo.GetByIdAsync(order.Id))!.Status);
    }

    private sealed class FakeLiqPayPort : ILiqPayPort
    {
        public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayCreatePaymentResult(true, request.OrderNumber, "https://liqpay.test", "d", "s", "{}", null));
        public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default) => Task.FromResult(true);
        public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
            => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, "success", "{}", null));
        public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));
        public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default)
            => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));
        public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Dictionary<long, Order> _items = new();
        private long _nextId = 1;
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>(_items.Values.Where(x => x.CustomerId == customerId).ToList());
        public Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            var id = order.Id.Value <= 0 ? _nextId++ : order.Id.Value;
            var saved = Order.Reconstitute(OrderId.From(id), order.OrderNumber, order.CustomerId, order.CompanyId, order.Status, order.TotalPrice, order.Subtotal, order.ShippingCost, order.DiscountAmount, order.TaxAmount, order.ShippingMethodId, order.PaymentMethod, order.Notes, order.TrackingNumber, order.ShippedAt, order.DeliveredAt, order.CancelledAt, order.RefundedAt, order.CreatedAt, order.UpdatedAt, order.IsDeleted, order.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }
        public Task UpdateAsync(Order order, CancellationToken ct = default) { _items[order.Id.Value] = order; return Task.CompletedTask; }
    }

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly Dictionary<long, Payment> _items = new();
        private long _nextId = 1;
        public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default) => Task.FromResult(_items.GetValueOrDefault(id.Value));
        public Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.OrderId == orderId));
        public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default) => Task.FromResult(_items.Values.FirstOrDefault(x => x.TransactionId == transactionId));
        public Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Payment>>(_items.Values.Where(x => x.Status == status).ToList());
        public Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
        {
            var id = payment.Id.Value <= 0 ? _nextId++ : payment.Id.Value;
            var saved = Payment.Reconstitute(PaymentId.From(id), payment.OrderId, payment.PaymentMethod, payment.Amount, payment.Currency, payment.TransactionId, payment.Status, payment.ProviderResponse, payment.ProcessedAt, payment.CreatedAt, payment.UpdatedAt, payment.IsDeleted, payment.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }
        public Task UpdateAsync(Payment payment, CancellationToken ct = default) { _items[payment.Id.Value] = payment; return Task.CompletedTask; }
    }

    private sealed class InMemoryRefundRepository : IRefundRepository
    {
        public List<Refund> Items { get; } = [];
        private long _nextId = 1;
        public Task<Refund?> GetByIdAsync(RefundId id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Refund>> ListByStatusAsync(RefundStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Refund>>(Items.Where(x => x.Status == status).ToList());
        public Task<Refund> AddAsync(Refund refund, CancellationToken ct = default)
        {
            var saved = Refund.Reconstitute(RefundId.From(_nextId++), refund.PaymentId, refund.OrderId, refund.Amount, refund.Reason, refund.Status, refund.ProcessedByUserId, refund.ProcessedAt, refund.CreatedAt, refund.UpdatedAt, refund.IsDeleted, refund.DeletedAt);
            Items.Add(saved);
            return Task.FromResult(saved);
        }
        public Task UpdateAsync(Refund refund, CancellationToken ct = default) => Task.CompletedTask;
    }
}
