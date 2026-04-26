using Marketplace.Application.Orders.DTOs;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Orders.Queries.ListOrders;

public enum OrderListScope
{
    My,
    Company,
    Admin
}

public sealed record ListOrdersQuery(
    OrderListScope Scope,
    Guid ActorUserId,
    bool IsActorAdmin,
    Guid? CompanyId,
    IReadOnlyList<OrderStatus>? Statuses,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    string? Search,
    string? Sort,
    int Page,
    int PageSize) : IRequest<Result<PagedOrdersDto>>;
