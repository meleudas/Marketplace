using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Repositories;

public interface ISellerLedgerRepository
{
    Task<SellerLedgerEntry?> GetByIdAsync(SellerLedgerEntryId id, CancellationToken ct = default);

    Task<bool> ExistsForOrderAndTypeAsync(
        OrderId orderId,
        SellerLedgerEntryType entryType,
        CancellationToken ct = default);

    Task<IReadOnlyList<SellerLedgerEntry>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);

    Task<IReadOnlyList<SellerLedgerEntry>> ListAvailableForSettlementAsync(
        CompanyId companyId,
        DateTime asOfUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<SellerLedgerEntry>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);

    Task<IReadOnlyList<CompanyId>> ListCompanyIdsWithPendingSaleEntriesAsync(DateTime asOfUtc, CancellationToken ct = default);

    Task<SellerLedgerEntry> AddAsync(SellerLedgerEntry entry, CancellationToken ct = default);

    Task AddRangeAsync(IReadOnlyList<SellerLedgerEntry> entries, CancellationToken ct = default);

    Task UpdateAsync(SellerLedgerEntry entry, CancellationToken ct = default);

    Task UpdateRangeAsync(IReadOnlyList<SellerLedgerEntry> entries, CancellationToken ct = default);
}
