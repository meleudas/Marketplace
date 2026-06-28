using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderFinancialsRepository : IOrderFinancialsRepository
{
    private readonly ApplicationDbContext _context;

    public OrderFinancialsRepository(ApplicationDbContext context) => _context = context;

    public async Task<OrderFinancials?> GetByIdAsync(OrderFinancialsId id, CancellationToken ct = default)
    {
        var row = await _context.OrderFinancials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<OrderFinancials?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var row = await _context.OrderFinancials.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<OrderFinancials?> GetByPaymentIdAsync(PaymentId paymentId, CancellationToken ct = default)
    {
        var row = await _context.OrderFinancials.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == paymentId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<OrderFinancials> AddAsync(OrderFinancials financials, CancellationToken ct = default)
    {
        var row = ToRecord(financials);
        await _context.OrderFinancials.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    private static OrderFinancials ToDomain(OrderFinancialsRecord row) =>
        OrderFinancials.Reconstitute(
            OrderFinancialsId.From(row.Id),
            OrderId.From(row.OrderId),
            PaymentId.From(row.PaymentId),
            CompanyId.From(row.CompanyId),
            row.Currency,
            row.MerchandiseSubtotal,
            row.DiscountAmount,
            row.MerchandiseBase,
            row.CommissionPercent,
            row.PlatformFee,
            row.SellerMerchandiseNet,
            row.ShippingAmount,
            row.SellerPayoutEligible,
            row.PostedAtUtc,
            row.CreatedAt,
            row.UpdatedAt);

    private static OrderFinancialsRecord ToRecord(OrderFinancials x) =>
        new()
        {
            Id = x.Id.Value,
            OrderId = x.OrderId.Value,
            PaymentId = x.PaymentId.Value,
            CompanyId = x.CompanyId.Value,
            Currency = x.Currency,
            MerchandiseSubtotal = x.MerchandiseSubtotal,
            DiscountAmount = x.DiscountAmount,
            MerchandiseBase = x.MerchandiseBase,
            CommissionPercent = x.CommissionPercent,
            PlatformFee = x.PlatformFee,
            SellerMerchandiseNet = x.SellerMerchandiseNet,
            ShippingAmount = x.ShippingAmount,
            SellerPayoutEligible = x.SellerPayoutEligible,
            PostedAtUtc = x.PostedAtUtc,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
}
