using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class RemainingDomainsSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.CompanyLegalProfiles.AnyAsync(ct) || await context.ReturnRequests.AnyAsync(ct))
            return;

        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var companies = await context.Companies.ToListAsync(ct);
        var orders = await context.Orders.Where(o => o.Status == 4).ToListAsync(ct);
        var orderFinancials = await context.OrderFinancials.ToListAsync(ct);
        var payments = await context.Payments.ToListAsync(ct);
        var sellers = await context.MarketplaceUsers.Where(u => u.Role == 2).ToListAsync(ct);
        var admin = await context.MarketplaceUsers.FirstAsync(u => u.Role == 4, ct);
        var orderItems = await context.OrderItems.ToListAsync(ct);

        foreach (var company in companies)
        {
            context.CompanyLegalProfiles.Add(new CompanyLegalProfileRecord
            {
                CompanyId = company.Id, LegalName = company.Name, LegalType = 1,
                Edrpou = $"{rng.Next(10000000, 99999999)}", IsVatPayer = rng.Next(2) == 0,
                PayoutIban = $"UA{rng.NextInt64(100000000000000000, 999999999999999999)}",
                PayoutRecipientName = company.Name, CreatedAt = now, UpdatedAt = now,
            });

            var contract = new CompanyContractRecord
            {
                CompanyId = company.Id, ContractNumber = $"CTR-{company.Name[..Math.Min(4, company.Name.Length)]}-{rng.Next(1000, 9999)}",
                Status = 1, EffectiveFrom = now.AddMonths(-6), SignedAt = now.AddMonths(-6), CreatedAt = now, UpdatedAt = now,
            };
            context.CompanyContracts.Add(contract);
            await context.SaveChangesAsync(ct);

            context.CompanyCommissionRates.Add(new CompanyCommissionRateRecord
            {
                CompanyId = company.Id, ContractId = contract.Id, CommissionPercent = 0.15m,
                EffectiveFrom = now.AddMonths(-6), CreatedAt = now, UpdatedAt = now,
            });

            var batch = new SettlementBatchRecord
            {
                CompanyId = company.Id, PeriodStartUtc = now.AddDays(-14), PeriodEndUtc = now,
                Status = 1, TotalAmount = 0, CreatedAt = now, UpdatedAt = now,
            };
            context.SettlementBatches.Add(batch);
            await context.SaveChangesAsync(ct);

            var payout = new SellerPayoutRecord
            {
                CompanyId = company.Id, SettlementBatchId = batch.Id, Status = 1, Amount = 0,
                Iban = $"UA{rng.NextInt64(100000000000000000, 999999999999999999)}",
                RecipientName = company.Name, CreatedAt = now, UpdatedAt = now,
            };
            context.SellerPayouts.Add(payout);
            await context.SaveChangesAsync(ct);
        }

        foreach (var order in orders)
        {
            var fin = orderFinancials.FirstOrDefault(f => f.OrderId == order.Id);
            context.SellerLedgerEntries.Add(new SellerLedgerEntryRecord
            {
                CompanyId = order.CompanyId, OrderId = order.Id, OrderFinancialsId = fin?.Id,
                EntryType = 1, Status = 1, Amount = order.TotalPrice, Currency = "UAH",
                Description = $"Order {order.OrderNumber}", AvailableAtUtc = now,
                CreatedAt = now, UpdatedAt = now,
            });
        }

        var returnOrders = orders.OrderBy(_ => rng.Next()).Take(3).ToList();
        foreach (var order in returnOrders)
        {
            var payment = payments.FirstOrDefault(p => p.OrderId == order.Id);
            if (payment == null) continue;

            var refund = new RefundRecord
            {
                PaymentId = payment.Id, OrderId = order.Id, Amount = order.TotalPrice * 0.5m,
                Reason = "Повернення товару", Status = 2, ProcessedByUserId = admin.Id,
                ProcessedAt = now, CreatedAt = now, UpdatedAt = now,
            };
            context.Refunds.Add(refund);
            await context.SaveChangesAsync(ct);

            var ret = new ReturnRequestRecord
            {
                OrderId = order.Id, CustomerId = order.CustomerId, CompanyId = order.CompanyId,
                Status = 2, ReasonCode = 2, Comment = "Не підійшов формат",
                ApprovedByUserId = admin.Id, ReceivedAtUtc = now, RefundId = refund.Id,
                CreatedAt = now, UpdatedAt = now,
            };
            context.ReturnRequests.Add(ret);
            await context.SaveChangesAsync(ct);

            var items = orderItems.Where(oi => oi.OrderId == order.Id).ToList();
            foreach (var item in items)
                context.ReturnLineItems.Add(new ReturnLineItemRecord
                {
                    ReturnRequestId = ret.Id, OrderItemId = item.Id, Quantity = item.Quantity,
                    Reason = "Не підійшов формат", CreatedAt = now, UpdatedAt = now,
                });
        }

        await context.SaveChangesAsync(ct);
    }
}
