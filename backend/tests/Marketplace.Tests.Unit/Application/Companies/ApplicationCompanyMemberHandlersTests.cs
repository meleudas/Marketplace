using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;
using Marketplace.Application.Companies.Queries.GetCompanyMembers;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Tests;

[Trait("Suite", "CompaniesWorkspace")]
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

    [Fact]
    public async Task AssignRole_Forbids_NonAdmin_Promote_To_Owner()
    {
        var companyRepo = new InMemoryCompanyRepository();
        var membersRepo = new InMemoryCompanyMemberRepository();
        var usersRepo = new InMemoryUserRepository();
        var companyId = CompanyId.From(Guid.NewGuid());
        var managerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        companyRepo.Seed(Company.Create(companyId, "C", "c", "d", null, "mail", "phone", Address.Empty, JsonBlob.Empty));
        membersRepo.AddSeed(CompanyMember.Create(companyId, managerId, CompanyMembershipRole.Manager));
        usersRepo.Add(targetId);

        var handler = new AssignCompanyMemberRoleCommandHandler(companyRepo, membersRepo, usersRepo);
        var result = await handler.Handle(
            new AssignCompanyMemberRoleCommand(companyId.Value, targetId, CompanyMembershipRole.Owner, managerId, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("forbidden", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangeRole_Forbids_NonAdmin_Promote_To_Owner()
    {
        var repo = new InMemoryCompanyMemberRepository();
        var companyId = CompanyId.From(Guid.NewGuid());
        var managerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        repo.AddSeed(CompanyMember.Create(companyId, managerId, CompanyMembershipRole.Manager));
        repo.AddSeed(CompanyMember.Create(companyId, targetId, CompanyMembershipRole.Seller));

        var handler = new ChangeCompanyMemberRoleCommandHandler(repo);
        var result = await handler.Handle(
            new ChangeCompanyMemberRoleCommand(companyId.Value, targetId, CompanyMembershipRole.Owner, managerId, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("forbidden", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMembers_Forbids_CrossCompany_Manager()
    {
        var repo = new InMemoryCompanyMemberRepository();
        var companyA = CompanyId.From(Guid.NewGuid());
        var companyB = CompanyId.From(Guid.NewGuid());
        var managerA = Guid.NewGuid();
        repo.AddSeed(CompanyMember.Create(companyA, managerA, CompanyMembershipRole.Manager));
        repo.AddSeed(CompanyMember.Create(companyB, Guid.NewGuid(), CompanyMembershipRole.Seller));

        var handler = new GetCompanyMembersQueryHandler(repo);
        var result = await handler.Handle(new GetCompanyMembersQuery(companyB.Value, managerA, false), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("forbidden", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryCompanyMemberRepository : ICompanyMemberRepository
    {
        private readonly Dictionary<(Guid CompanyId, Guid UserId), CompanyMember> _items = new();

        public void AddSeed(CompanyMember member) => _items[(member.CompanyId.Value, member.UserId)] = member;

        public Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault((companyId.Value, userId)));

        public Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyMember>>(_items.Values.Where(x => x.UserId == userId && !x.IsDeleted).ToList());

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

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private readonly Dictionary<Guid, Company> _items = new();
        public void Seed(Company company) => _items[company.Id.Value] = company;

        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<Company?> GetApprovedNotDeletedBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.Slug == slug && x.IsApproved && !x.IsDeleted));

        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.ToList());

        public Task<IReadOnlyList<Company>> GetApprovedAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.Where(x => x.IsApproved && !x.IsDeleted).ToList());

        public Task<IReadOnlyList<Company>> GetPendingApprovalAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.Where(x => !x.IsApproved && !x.IsDeleted).ToList());

        public Task AddAsync(Company company, CancellationToken ct = default)
        {
            _items[company.Id.Value] = company;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Company company, CancellationToken ct = default)
        {
            _items[company.Id.Value] = company;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _items = new();

        public void Add(Guid identityId)
            => _items[identityId] = User.Create(IdentityUserId.From(identityId), "First", "Last", UserRole.Seller);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<User?> GetByIdentityIdAsync(IdentityUserId identityId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(identityId.Value));

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(_items.Values.ToList());

        public Task<IReadOnlyList<User>> SearchByUserNameAsync(string userName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>([]);

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            _items[user.Id.Value] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _items[user.Id.Value] = user;
            return Task.CompletedTask;
        }
    }
}
