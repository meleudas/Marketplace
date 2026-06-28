using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Entities;
using Marketplace.Domain.Returns.Enums;

namespace Marketplace.Domain.Returns.Repositories;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken ct = default);
    Task<IReadOnlyList<ReturnRequest>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<ReturnRequest>> ListByCompanyAsync(CompanyId companyId, ReturnRequestStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<ReturnRequest>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task<ReturnRequest> AddAsync(ReturnRequest entity, CancellationToken ct = default);
    Task UpdateAsync(ReturnRequest entity, CancellationToken ct = default);
}
