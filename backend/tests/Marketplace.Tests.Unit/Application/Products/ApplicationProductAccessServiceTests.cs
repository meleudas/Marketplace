using Marketplace.Application.Products.Authorization;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

public class ApplicationProductAccessServiceTests
{
    [Theory]
    [InlineData(CompanyMembershipRole.Owner, true)]
    [InlineData(CompanyMembershipRole.Manager, true)]
    [InlineData(CompanyMembershipRole.Seller, true)]
    [InlineData(CompanyMembershipRole.Support, false)]
    [InlineData(CompanyMembershipRole.Logistics, false)]
    public async Task WritePermission_Depends_On_Company_Role(CompanyMembershipRole role, bool expected)
    {
        var repo = new InMemoryCompanyMemberRepository();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repo.Seed(CompanyMember.Create(CompanyId.From(companyId), userId, role));
        var service = new ProductAccessService(repo);

        var actual = await service.HasAccessAsync(companyId, userId, false, ProductPermission.WriteProduct, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Admin_Has_Global_Bypass()
    {
        var service = new ProductAccessService(new InMemoryCompanyMemberRepository());
        var result = await service.HasAccessAsync(Guid.NewGuid(), Guid.NewGuid(), true, ProductPermission.WriteProduct, CancellationToken.None);
        Assert.True(result);
    }

    private sealed class InMemoryCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly Dictionary<(Guid CompanyId, Guid UserId), CompanyMember> _items = new();
        public void Seed(CompanyMember member) => _items[(member.CompanyId.Value, member.UserId)] = member;
        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault((companyId.Value, userId)));
        public Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>(_items.Values.Where(x => x.UserId == userId && !x.IsDeleted).ToList());
        public Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>(_items.Values.Where(x => x.CompanyId == companyId && !x.IsDeleted).ToList());
        public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.Any(x => x.CompanyId == companyId && x.IsOwner && !x.IsDeleted));
        public Task AddAsync(CompanyMember member, CancellationToken ct = default) { Seed(member); return Task.CompletedTask; }
        public Task UpdateAsync(CompanyMember member, CancellationToken ct = default) { Seed(member); return Task.CompletedTask; }
    }
}
