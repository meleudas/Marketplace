using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;
using Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Queries.GetCompanyMembers;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "CompaniesWorkspace")]
public class IntegrationCompaniesWorkspaceSqliteTests
{
    [Fact]
    public async Task Create_Approve_SetCommission_Creates_Contract_And_Rate_In_Db()
    {
        await using var db = await CreateSqliteContextAsync();
        var cache = new NoopCachePort();

        var companyRepo = new CompanyRepository(db);
        var legalRepo = new CompanyLegalProfileRepository(db);
        var contractRepo = new CompanyContractRepository(db);
        var rateRepo = new CompanyCommissionRateRepository(db);

        var create = new CreateCompanyCommandHandler(companyRepo, legalRepo, cache);
        var createResult = await create.Handle(
            new CreateCompanyCommand(
                "Workspace Co",
                "workspace-co",
                "desc",
                null,
                "mail@example.com",
                "+380001112233",
                new CompanyAddressDto("Street", "City", "Region", "01001", "UA"),
                new CompanyLegalProfileDto("Workspace LLC", "llc", "12345678", null, null, true, 11m),
                "{}"),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);
        var companyId = createResult.Value!.Id;

        var approve = new ApproveCompanyCommandHandler(companyRepo, legalRepo, contractRepo, rateRepo, cache);
        var approveResult = await approve.Handle(new ApproveCompanyCommand(companyId, Guid.NewGuid()), CancellationToken.None);
        Assert.True(approveResult.IsSuccess);

        var setRate = new SetCompanyCommissionRateCommandHandler(companyRepo, contractRepo, rateRepo, cache);
        var setResult = await setRate.Handle(
            new SetCompanyCommissionRateCommand(companyId, 15m, DateTime.UtcNow.AddDays(1), "raise", Guid.NewGuid()),
            CancellationToken.None);
        Assert.True(setResult.IsSuccess);

        var contract = await contractRepo.GetActiveByCompanyIdAsync(CompanyId.From(companyId), CancellationToken.None);
        var rate = await rateRepo.GetActiveByCompanyIdAsync(CompanyId.From(companyId), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.NotNull(rate);
    }

    [Fact]
    public async Task Membership_Flow_Enforces_Tenant_Isolation_And_LastOwner_Guard()
    {
        await using var db = await CreateSqliteContextAsync();
        var companyRepo = new CompanyRepository(db);
        var memberRepo = new CompanyMemberRepository(db);
        var users = new FakeUserRepository();

        var companyA = Company.Create(CompanyId.From(Guid.NewGuid()), "A", "a", "d", null, "a@ex.com", "1", Address.Empty, JsonBlob.Empty);
        var companyB = Company.Create(CompanyId.From(Guid.NewGuid()), "B", "b", "d", null, "b@ex.com", "1", Address.Empty, JsonBlob.Empty);
        await companyRepo.AddAsync(companyA, CancellationToken.None);
        await companyRepo.AddAsync(companyB, CancellationToken.None);

        var ownerA = Guid.NewGuid();
        var managerA = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var outsiderB = Guid.NewGuid();
        users.Seed(ownerA, managerA, sellerA, outsiderB);

        await memberRepo.AddAsync(CompanyMember.Create(companyA.Id, ownerA, CompanyMembershipRole.Owner), CancellationToken.None);
        await memberRepo.AddAsync(CompanyMember.Create(companyA.Id, managerA, CompanyMembershipRole.Manager), CancellationToken.None);
        await memberRepo.AddAsync(CompanyMember.Create(companyB.Id, outsiderB, CompanyMembershipRole.Manager), CancellationToken.None);

        var assign = new AssignCompanyMemberRoleCommandHandler(companyRepo, memberRepo, users);
        var assignResult = await assign.Handle(
            new AssignCompanyMemberRoleCommand(companyA.Id.Value, sellerA, CompanyMembershipRole.Seller, managerA, false),
            CancellationToken.None);
        Assert.True(assignResult.IsSuccess);

        var query = new GetCompanyMembersQueryHandler(memberRepo);
        var isolationResult = await query.Handle(new GetCompanyMembersQuery(companyA.Id.Value, outsiderB, false), CancellationToken.None);
        Assert.True(isolationResult.IsFailure);
        Assert.Contains("forbidden", isolationResult.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var change = new ChangeCompanyMemberRoleCommandHandler(memberRepo);
        var changeResult = await change.Handle(
            new ChangeCompanyMemberRoleCommand(companyA.Id.Value, sellerA, CompanyMembershipRole.Logistics, managerA, false),
            CancellationToken.None);
        Assert.True(changeResult.IsSuccess);

        var remove = new RemoveCompanyMemberCommandHandler(memberRepo);
        var removeOwner = await remove.Handle(
            new RemoveCompanyMemberCommand(companyA.Id.Value, ownerA, ownerA, true),
            CancellationToken.None);
        Assert.True(removeOwner.IsFailure);
        Assert.Contains("last owner", removeOwner.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = new();
        public void Seed(params Guid[] ids)
        {
            foreach (var id in ids)
                _users[id] = User.Create(IdentityUserId.From(id), "F", "L", UserRole.Seller);
        }

        public Task<User?> GetByIdAsync(Marketplace.Domain.Users.ValueObjects.UserId id, CancellationToken ct = default)
            => Task.FromResult(_users.GetValueOrDefault(id.Value));

        public Task<User?> GetByIdentityIdAsync(IdentityUserId identityId, CancellationToken ct = default)
            => Task.FromResult(_users.GetValueOrDefault(identityId.Value));

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

        public Task<IReadOnlyList<User>> SearchByUserNameAsync(string userName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>([]);

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            _users[user.Id.Value] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _users[user.Id.Value] = user;
            return Task.CompletedTask;
        }
    }
}
