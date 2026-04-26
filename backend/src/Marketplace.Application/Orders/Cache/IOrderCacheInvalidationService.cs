namespace Marketplace.Application.Orders.Cache;

public interface IOrderCacheInvalidationService
{
    Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default);
    Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default);
}
