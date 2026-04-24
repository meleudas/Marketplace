using Marketplace.Application.Reviews.Authorization;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

public sealed class ApplicationReviewAccessServiceTests
{
    [Fact]
    public async Task ReplyAsCompany_Allows_Owner_Manager_Seller()
    {
        var companyId = CompanyId.From(Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var repository = new StubCompanyMemberRepository(
            CompanyMember.Create(companyId, actorId, CompanyMembershipRole.Seller));
        var service = new ReviewAccessService(repository);

        var allowed = await service.HasCompanyAccessAsync(companyId, actorId, false, ReviewPermission.ReplyAsCompany);

        Assert.True(allowed);
    }

    [Fact]
    public async Task ReplyAsCompany_Denies_Support_And_Logistics()
    {
        var companyId = CompanyId.From(Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var repository = new StubCompanyMemberRepository(
            CompanyMember.Create(companyId, actorId, CompanyMembershipRole.Support));
        var service = new ReviewAccessService(repository);

        var allowed = await service.HasCompanyAccessAsync(companyId, actorId, false, ReviewPermission.ReplyAsCompany);

        Assert.False(allowed);
    }

    private sealed class StubCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly CompanyMember? _member;

        public StubCompanyMemberRepository(CompanyMember? member) => _member = member;

        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_member is not null && _member.CompanyId == companyId && _member.UserId == userId ? _member : null);

        public Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CompanyMember>>([]);

        public Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CompanyMember>>([]);

        public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(CompanyMember member, CancellationToken ct = default) => Task.CompletedTask;
    }
}
