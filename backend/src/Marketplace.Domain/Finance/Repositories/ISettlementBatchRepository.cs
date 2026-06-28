using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Repositories;

public interface ISettlementBatchRepository
{
    Task<SettlementBatch?> GetByIdAsync(SettlementBatchId id, CancellationToken ct = default);

    Task<SettlementBatch?> GetOpenByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);

    Task<IReadOnlyList<SettlementBatch>> ListByStatusAsync(SettlementBatchStatus status, CancellationToken ct = default);

    Task<IReadOnlyList<SettlementBatch>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default);

    Task<IReadOnlyList<SettlementBatch>> ListAsync(
        SettlementBatchStatus? status,
        CompanyId? companyId,
        CancellationToken ct = default);

    Task<SettlementBatch> AddAsync(SettlementBatch batch, CancellationToken ct = default);

    Task UpdateAsync(SettlementBatch batch, CancellationToken ct = default);
}
