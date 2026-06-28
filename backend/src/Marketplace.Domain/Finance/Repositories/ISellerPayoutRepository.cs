using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Repositories;

public interface ISellerPayoutRepository
{
    Task<SellerPayout?> GetByIdAsync(SellerPayoutId id, CancellationToken ct = default);

    Task<SellerPayout?> GetBySettlementBatchIdAsync(SettlementBatchId settlementBatchId, CancellationToken ct = default);

    Task<IReadOnlyList<SellerPayout>> ListByStatusAsync(SellerPayoutStatus status, CancellationToken ct = default);

    Task<SellerPayout> AddAsync(SellerPayout payout, CancellationToken ct = default);

    Task UpdateAsync(SellerPayout payout, CancellationToken ct = default);
}
