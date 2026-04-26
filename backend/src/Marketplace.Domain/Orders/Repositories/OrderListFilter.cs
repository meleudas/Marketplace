using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Domain.Orders.Repositories;

public sealed record OrderListFilter(
    Guid? CustomerId,
    Guid? CompanyId,
    IReadOnlyList<OrderStatus>? Statuses,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
