using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Options;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Finance.Services;

public sealed class OrderFinancialsWriter : IOrderFinancialsWriter
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICompanyCommissionRateRepository _commissionRateRepository;
    private readonly IOrderFinancialsRepository _orderFinancialsRepository;
    private readonly SellerLedgerService _sellerLedgerService;
    private readonly SettlementOptions _settlementOptions;

    public OrderFinancialsWriter(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ICompanyCommissionRateRepository commissionRateRepository,
        IOrderFinancialsRepository orderFinancialsRepository,
        SellerLedgerService sellerLedgerService,
        IOptions<SettlementOptions> settlementOptions)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _commissionRateRepository = commissionRateRepository;
        _orderFinancialsRepository = orderFinancialsRepository;
        _sellerLedgerService = sellerLedgerService;
        _settlementOptions = settlementOptions.Value;
    }

    public async Task PostOnPaymentCompletedAsync(PaymentId paymentId, CancellationToken ct = default)
    {
        var existing = await _orderFinancialsRepository.GetByPaymentIdAsync(paymentId, ct);
        if (existing is not null)
            return;

        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return;

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null)
            return;

        var postedAt = payment.ProcessedAt ?? DateTime.UtcNow;
        var commissionRate = await _commissionRateRepository.GetActiveAtAsync(order.CompanyId, postedAt, ct);
        var commissionPercent = commissionRate?.CommissionPercent
            ?? throw new InvalidOperationException($"No active commission rate for company '{order.CompanyId.Value}' at {postedAt:O}");

        var calculation = CommissionCalculator.Calculate(
            order.Subtotal.Amount,
            order.DiscountAmount.Amount,
            order.ShippingCost.Amount,
            commissionPercent);

        var financials = OrderFinancials.Create(
            OrderFinancialsId.From(0),
            order.Id,
            payment.Id,
            order.CompanyId,
            payment.Currency,
            calculation.MerchandiseSubtotal,
            calculation.DiscountAmount,
            calculation.MerchandiseBase,
            calculation.CommissionPercent,
            calculation.PlatformFee,
            calculation.SellerMerchandiseNet,
            calculation.ShippingAmount,
            calculation.SellerPayoutEligible,
            postedAt);

        var saved = await _orderFinancialsRepository.AddAsync(financials, ct);
        await _sellerLedgerService.PostPaymentEntriesAsync(saved, order, _settlementOptions.HoldDaysAfterDelivery, ct);

        MarketplaceMetrics.CommissionPosted.Add(1, [
            new KeyValuePair<string, object?>("company_id", order.CompanyId.Value.ToString())
        ]);
    }

    public async Task PostRefundReversalAsync(
        PaymentId paymentId,
        decimal refundAmount,
        string reason,
        CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return;

        var financials = await _orderFinancialsRepository.GetByPaymentIdAsync(paymentId, ct);
        if (financials is null)
            return;

        await _sellerLedgerService.PostRefundReversalAsync(financials, payment.Amount.Amount, refundAmount, reason, ct);
    }
}
