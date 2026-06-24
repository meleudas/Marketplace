using Marketplace.Application.Common.Options;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Policies;
using Marketplace.Application.Orders.Queries.ListOrders;
using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Tests.Common.Fakes;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class ApplicationListOrdersQueryHandlerTests
{
    [Fact]
    public async Task Admin_Scope_Passes_CompanyId_To_Repository_Filter()
    {
        var companyId = Guid.NewGuid();
        var repo = new CapturingOrderRepository();
        var handler = CreateHandler(repo, new StubCompanyMemberRepository(null));

        var result = await handler.Handle(
            new ListOrdersQuery(
                OrderListScope.Admin,
                Guid.NewGuid(),
                true,
                companyId,
                null,
                null,
                null,
                null,
                null,
                null,
                1,
                20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastFilter);
        Assert.Equal(companyId, repo.LastFilter!.CompanyId);
        Assert.Null(repo.LastFilter.CustomerId);
    }

    [Fact]
    public async Task Company_Scope_Rejects_Invalid_CompanyMemberId()
    {
        var companyId = Guid.NewGuid();
        var handler = CreateHandler(new CapturingOrderRepository(), new StubCompanyMemberRepository(null));

        var result = await handler.Handle(
            new ListOrdersQuery(
                OrderListScope.Company,
                Guid.NewGuid(),
                false,
                companyId,
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null,
                1,
                20),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid companyMemberId", result.Error);
    }

    [Fact]
    public async Task My_Scope_Rejects_CompanyMemberId()
    {
        var handler = CreateHandler(new CapturingOrderRepository(), new StubCompanyMemberRepository(null));

        var result = await handler.Handle(
            new ListOrdersQuery(
                OrderListScope.My,
                Guid.NewGuid(),
                false,
                null,
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null,
                1,
                20),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("companyMemberId", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static ListOrdersQueryHandler CreateHandler(
        IOrderRepository repo,
        ICompanyMemberRepository companyMembers) =>
        new(
            repo,
            companyMembers,
            new VersionTrackingCachePort(),
            Options.Create(new CacheTtlOptions { OrdersListMinutes = 5 }),
            new AllowAccessService(),
            new OrderCacheInvalidationService(new VersionTrackingCachePort()));

    private sealed class CapturingOrderRepository : IOrderRepository
    {
        public OrderListFilter? LastFilter { get; private set; }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult<Order?>(null);

        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Order>>([]);

        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default)
        {
            LastFilter = filter;
            return Task.FromResult<(IReadOnlyList<Order>, long)>(([], 0));
        }

        public Task<Order> AddAsync(Order order, CancellationToken ct = default) => Task.FromResult(order);

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class AllowAccessService : IOrderAccessService
    {
        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<OrderCancellationActor> ResolveCancellationActorAsync(Order order, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(OrderCancellationActor.CompanyMember);
    }

    private sealed class StubCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly CompanyMember? _member;

        public StubCompanyMemberRepository(CompanyMember? member) => _member = member;

        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_member is not null && _member.CompanyId == companyId && _member.UserId == userId ? _member : null);

        public Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>([]);

        public Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>([]);

        public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult(false);

        public Task AddAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
    }
}
