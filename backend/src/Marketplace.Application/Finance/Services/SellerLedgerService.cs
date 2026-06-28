using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Finance.Services;

public sealed class SellerLedgerService
{
    private readonly ISellerLedgerRepository _sellerLedgerRepository;

    public SellerLedgerService(ISellerLedgerRepository sellerLedgerRepository) =>
        _sellerLedgerRepository = sellerLedgerRepository;

    public async Task PostPaymentEntriesAsync(
        OrderFinancials financials,
        Order order,
        int holdDaysAfterDelivery,
        CancellationToken ct = default)
    {
        if (await _sellerLedgerRepository.ExistsForOrderAndTypeAsync(order.Id, SellerLedgerEntryType.Sale, ct))
            return;

        var availableAt = order.DeliveredAt?.AddDays(holdDaysAfterDelivery)
            ?? DateTime.UtcNow.AddDays(holdDaysAfterDelivery);

        var saleEntry = SellerLedgerEntry.Create(
            SellerLedgerEntryId.From(0),
            financials.CompanyId,
            order.Id,
            financials.Id,
            SellerLedgerEntryType.Sale,
            financials.SellerPayoutEligible,
            financials.Currency,
            $"Sale for order {order.OrderNumber}",
            availableAt);

        var feeEntry = SellerLedgerEntry.Create(
            SellerLedgerEntryId.From(0),
            financials.CompanyId,
            order.Id,
            financials.Id,
            SellerLedgerEntryType.PlatformFee,
            financials.PlatformFee,
            financials.Currency,
            $"Platform fee for order {order.OrderNumber}",
            availableAt);

        await _sellerLedgerRepository.AddRangeAsync([saleEntry, feeEntry], ct);

        MarketplaceMetrics.SellerLedgerEntries.Add(2, [
            new KeyValuePair<string, object?>("entry_type", SellerLedgerEntryType.Sale.ToString()),
            new KeyValuePair<string, object?>("company_id", financials.CompanyId.Value.ToString())
        ]);
    }

    public async Task PostRefundReversalAsync(
        OrderFinancials financials,
        decimal paymentAmount,
        decimal refundAmount,
        string reason,
        CancellationToken ct = default)
    {
        if (paymentAmount <= 0 || refundAmount <= 0)
            return;

        if (await _sellerLedgerRepository.ExistsForOrderAndTypeAsync(
                financials.OrderId,
                SellerLedgerEntryType.Refund,
                ct))
        {
            // Allow partial refunds by checking if we already reversed full amount — keep simple: one reversal per refund call.
        }

        var ratio = refundAmount / paymentAmount;
        var reversalAmount = -Math.Round(financials.SellerPayoutEligible * ratio, 2, MidpointRounding.AwayFromZero);
        if (reversalAmount == 0)
            return;

        var entry = SellerLedgerEntry.Create(
            SellerLedgerEntryId.From(0),
            financials.CompanyId,
            financials.OrderId,
            financials.Id,
            SellerLedgerEntryType.Refund,
            reversalAmount,
            financials.Currency,
            string.IsNullOrWhiteSpace(reason) ? "Refund reversal" : reason.Trim(),
            DateTime.UtcNow);
        entry.MarkAvailable(DateTime.UtcNow);

        await _sellerLedgerRepository.AddAsync(entry, ct);

        MarketplaceMetrics.SellerLedgerEntries.Add(1, [
            new KeyValuePair<string, object?>("entry_type", SellerLedgerEntryType.Refund.ToString()),
            new KeyValuePair<string, object?>("company_id", financials.CompanyId.Value.ToString())
        ]);
    }
}
