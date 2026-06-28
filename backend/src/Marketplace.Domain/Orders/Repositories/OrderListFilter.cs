using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Domain.Orders.Repositories;

public sealed record OrderListFilter(
    Guid? CustomerId,
    Guid? CompanyId,
    Guid? CompanyMemberUserId,
    IReadOnlyList<OrderStatus>? Statuses,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
