using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Orders.Queries.ListOrders;

public sealed class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, Result<PagedOrdersDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICompanyMemberRepository _companyMembers;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;
    private readonly IOrderAccessService _access;
    private readonly IOrderCacheInvalidationService _cacheInvalidation;

    public ListOrdersQueryHandler(
        IOrderRepository orderRepository,
        ICompanyMemberRepository companyMembers,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl,
        IOrderAccessService access,
        IOrderCacheInvalidationService cacheInvalidation)
    {
        _orderRepository = orderRepository;
        _companyMembers = companyMembers;
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

        if (request.CompanyMemberUserId.HasValue && request.Scope == OrderListScope.My)
            return Result<PagedOrdersDto>.Failure("companyMemberId is not supported for buyer scope");

        if (request.CompanyMemberUserId.HasValue && request.Scope == OrderListScope.Company)
        {
            if (!request.CompanyId.HasValue)
                return Result<PagedOrdersDto>.Failure("companyMemberId requires company scope");

            var member = await _companyMembers.GetByCompanyAndUserAsync(
                CompanyId.From(request.CompanyId.Value),
                request.CompanyMemberUserId.Value,
                ct);
            if (member is null || member.IsDeleted)
                return Result<PagedOrdersDto>.Failure("Invalid companyMemberId");
        }

        var customerId = request.Scope == OrderListScope.My ? request.ActorUserId : null as Guid?;
        var companyId = request.Scope is OrderListScope.Company or OrderListScope.Admin
            ? request.CompanyId
            : null;
        var cacheScope = request.Scope.ToCacheScope();
        var listVersion = await _cacheInvalidation.GetListVersionAsync(
            cacheScope,
            request.Scope == OrderListScope.My ? request.ActorUserId : null,
            request.Scope == OrderListScope.Company ? request.CompanyId : null,
            ct);

        var key = OrderCacheKeys.List(
            listVersion,
            cacheScope,
            request.Scope == OrderListScope.My ? request.ActorUserId : null,
            companyId,
            request.CompanyMemberUserId,
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
                request.CompanyMemberUserId,
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
        await _cacheInvalidation.TrackListKeyAsync(
            cacheScope,
            request.Scope == OrderListScope.My ? request.ActorUserId : null,
            request.Scope == OrderListScope.Company ? request.CompanyId : null,
            key,
            _ttl.OrdersList,
            ct);
        return Result<PagedOrdersDto>.Success(dto);
    }
}
