using Marketplace.Application.Orders.Authorization;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Tests;

public sealed class ApplicationOrderAccessServiceTests
{
    [Fact]
    public async Task Buyer_Can_Read_And_Cancel_Own_Order()
    {
        var actorId = Guid.NewGuid();
        var order = BuildOrder(actorId, Guid.NewGuid());
        var service = new OrderAccessService(new StubCompanyMemberRepository(null));

        var canRead = await service.HasAccessAsync(order, actorId, false, OrderPermission.Read);
        var canCancel = await service.HasAccessAsync(order, actorId, false, OrderPermission.Cancel);

        Assert.True(canRead);
        Assert.True(canCancel);
    }

    [Fact]
    public async Task Seller_Member_Can_Manage_Status()
    {
        var companyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var order = BuildOrder(Guid.NewGuid(), companyId);
        var member = CompanyMember.Create(CompanyId.From(companyId), actorId, CompanyMembershipRole.Seller);
        var service = new OrderAccessService(new StubCompanyMemberRepository(member));

        var allowed = await service.HasAccessAsync(order, actorId, false, OrderPermission.ManageStatus);

        Assert.True(allowed);
    }

    private static Order BuildOrder(Guid customerId, Guid companyId)
        => Order.Reconstitute(
            OrderId.From(1),
            "ORD-1",
            customerId,
            CompanyId.From(companyId),
            OrderStatus.Pending,
            new Money(100),
            new Money(100),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(1),
            CheckoutPaymentMethod.Card,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private sealed class StubCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly CompanyMember? _member;
        public StubCompanyMemberRepository(CompanyMember? member) => _member = member;

        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_member is not null && _member.CompanyId == companyId && _member.UserId == userId ? _member : null);
        public Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CompanyMember>>([]);
        public Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CompanyMember>>([]);
        public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
    }
}
