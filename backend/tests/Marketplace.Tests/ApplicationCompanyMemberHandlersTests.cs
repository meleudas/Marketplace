using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

public class ApplicationCompanyMemberHandlersTests
{
    [Fact]
    public async Task ChangeRole_Fails_For_Last_Owner_Demotion()
    {
        var repo = new InMemoryCompanyMemberRepository();
        var companyId = CompanyId.From(Guid.NewGuid());
        var ownerId = Guid.NewGuid();
        repo.AddSeed(CompanyMember.Create(companyId, ownerId, CompanyMembershipRole.Owner));

        var handler = new ChangeCompanyMemberRoleCommandHandler(repo);
        var result = await handler.Handle(
            new ChangeCompanyMemberRoleCommand(companyId.Value, ownerId, CompanyMembershipRole.Manager, ownerId, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("last owner", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveMember_Fails_For_Last_Owner()
    {
        var repo = new InMemoryCompanyMemberRepository();
        var companyId = CompanyId.From(Guid.NewGuid());
        var ownerId = Guid.NewGuid();
        repo.AddSeed(CompanyMember.Create(companyId, ownerId, CompanyMembershipRole.Owner));

        var handler = new RemoveCompanyMemberCommandHandler(repo);
        var result = await handler.Handle(
            new RemoveCompanyMemberCommand(companyId.Value, ownerId, ownerId, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("last owner", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly Dictionary<(Guid CompanyId, Guid UserId), CompanyMember> _items = new();

        public void AddSeed(CompanyMember member) => _items[(member.CompanyId.Value, member.UserId)] = member;

        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault((companyId.Value, userId)));

        public Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>(_items.Values.Where(x => x.CompanyId.Value == companyId.Value && !x.IsDeleted).ToList());

        public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.Any(x => x.CompanyId.Value == companyId.Value && x.IsOwner && !x.IsDeleted));

        public Task AddAsync(CompanyMember member, CancellationToken ct = default)
        {
            _items[(member.CompanyId.Value, member.UserId)] = member;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CompanyMember member, CancellationToken ct = default)
        {
            _items[(member.CompanyId.Value, member.UserId)] = member;
            return Task.CompletedTask;
        }
    }
}
