using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Application.Orders.Cache;

public static class OrderCacheKeys
{
    public static string AdminListVersion() => "orders:list:version:admin";
    public static string CompanyListVersion(Guid companyId) => $"orders:list:version:company:{companyId}";
    public static string MyListVersion(Guid userId) => $"orders:list:version:my:{userId}";

    public static string List(
        long version,
        string scope,
        Guid? actorUserId,
        Guid? companyId,
        IReadOnlyList<OrderStatus>? statuses,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? search,
        string? sort,
        int page,
        int pageSize)
    {
        var statusPart = statuses is { Count: > 0 }
            ? string.Join(",", statuses.Select(x => x.ToString().ToLowerInvariant()))
            : "all";

        return $"orders:list:v{version}:{scope}:{actorUserId}:{companyId}:{statusPart}:{fromUtc:O}:{toUtc:O}:{(search ?? "").Trim().ToLowerInvariant()}:{(sort ?? "created_desc").Trim().ToLowerInvariant()}:{page}:{pageSize}";
    }

    public static string Detail(long orderId) => $"orders:detail:{orderId}";
}
