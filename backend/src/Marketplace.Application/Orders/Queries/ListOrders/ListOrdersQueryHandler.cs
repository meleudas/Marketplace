using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Orders.Queries.ListOrders;

public sealed class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, Result<PagedOrdersDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;
    private readonly IOrderAccessService _access;
    private readonly IOrderCacheInvalidationService _cacheInvalidation;

    public ListOrdersQueryHandler(
        IOrderRepository orderRepository,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl,
        IOrderAccessService access,
        IOrderCacheInvalidationService cacheInvalidation)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _ttl = ttl.Value;
        _access = access;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<Result<PagedOrdersDto>> Handle(ListOrdersQuery request, CancellationToken ct)
    {
        if (request.Scope == OrderListScope.Company && request.CompanyId.HasValue)
        {
            var canRead = await _access.CanReadCompanyScopeAsync(request.CompanyId.Value, request.ActorUserId, request.IsActorAdmin, ct);
            if (!canRead)
                return Result<PagedOrdersDto>.Failure("Forbidden");
        }

        if (request.Scope == OrderListScope.Admin && !request.IsActorAdmin)
            return Result<PagedOrdersDto>.Failure("Forbidden");

        var customerId = request.Scope == OrderListScope.My ? request.ActorUserId : null as Guid?;
        var companyId = request.Scope == OrderListScope.Company ? request.CompanyId : null;
        var listVersion = await _cacheInvalidation.GetListVersionAsync(
            request.Scope.ToString(),
            request.Scope == OrderListScope.My ? request.ActorUserId : null,
            request.Scope == OrderListScope.Company ? request.CompanyId : null,
            ct);

        var key = OrderCacheKeys.List(
            listVersion,
            request.Scope.ToString().ToLowerInvariant(),
            request.Scope == OrderListScope.My ? request.ActorUserId : null,
            companyId,
            request.Statuses,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            request.Search,
            request.Sort,
            request.Page,
            request.PageSize);

        var cached = await _cache.GetAsync<PagedOrdersDto>(key, ct);
        if (cached is not null)
            return Result<PagedOrdersDto>.Success(cached);

        var (items, total) = await _orderRepository.ListAsync(
            new OrderListFilter(
                customerId,
                companyId,
                request.Statuses,
                request.CreatedFromUtc,
                request.CreatedToUtc,
                request.Search,
                request.Sort,
                request.Page,
                request.PageSize),
            ct);

        var dto = new PagedOrdersDto(
            items.Select(x => new OrderListItemDto(
                x.Id.Value,
                x.OrderNumber,
                x.CustomerId,
                x.CompanyId.Value,
                x.Status,
                x.TotalPrice.Amount,
                x.PaymentMethod.ToString(),
                x.CreatedAt,
                x.UpdatedAt)).ToList(),
            total,
            request.Page,
            request.PageSize);

        await _cache.SetAsync(key, dto, _ttl.OrdersList, ct);
        return Result<PagedOrdersDto>.Success(dto);
    }
}
