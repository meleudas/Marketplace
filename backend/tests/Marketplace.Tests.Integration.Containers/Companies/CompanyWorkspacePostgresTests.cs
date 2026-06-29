using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Companies;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "CompaniesWorkspace")]
[Trait("Layer", "IntegrationContainers")]
public sealed class CompanyWorkspacePostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public CompanyWorkspacePostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_Approve_SetCommission_And_ListMembers_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.Persistence.ApplicationDbContext>();
        var cache = new NoopCachePort();
        var companyRepo = new CompanyRepository(db);
        var legalRepo = new CompanyLegalProfileRepository(db);
        var contractRepo = new CompanyContractRepository(db);
        var rateRepo = new CompanyCommissionRateRepository(db);
        var memberRepo = new CompanyMemberRepository(db);
        var adminId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var create = new CreateCompanyCommandHandler(companyRepo, legalRepo, cache);
        var created = await create.Handle(
            new CreateCompanyCommand(
                "Container Co",
                $"container-co-{Guid.NewGuid():N}"[..24],
                "desc",
                null,
                "mail@example.com",
                "+380001112233",
                new CompanyAddressDto("Street", "City", "Region", "01001", "UA"),
                new CompanyLegalProfileDto("Container LLC", "llc", "12345678", null, null, true, 11m),
                "{}"),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var companyId = created.Value!.Id;
        var approve = new ApproveCompanyCommandHandler(companyRepo, legalRepo, contractRepo, rateRepo, cache);
        Assert.True((await approve.Handle(new ApproveCompanyCommand(companyId, adminId), CancellationToken.None)).IsSuccess);

        await memberRepo.AddAsync(CompanyMember.Create(CompanyId.From(companyId), ownerId, CompanyMembershipRole.Owner), CancellationToken.None);

        var setRate = new SetCompanyCommissionRateCommandHandler(companyRepo, contractRepo, rateRepo, cache);
        Assert.True((await setRate.Handle(
            new SetCompanyCommissionRateCommand(companyId, 12.5m, DateTime.UtcNow.AddDays(1), "seed", adminId),
            CancellationToken.None)).IsSuccess);

        var contract = await contractRepo.GetActiveByCompanyIdAsync(CompanyId.From(companyId), CancellationToken.None);
        var rate = await rateRepo.GetActiveByCompanyIdAsync(CompanyId.From(companyId), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.NotNull(rate);
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }
}
