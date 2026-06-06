using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.RevokeCompanyApproval;
using Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "CompaniesWorkspace")]
public class ApplicationCompanyLifecycleHandlersTests
{
    [Fact]
    public async Task ApproveCompany_Creates_Contract_And_Initial_Commission()
    {
        var companyRepo = new InMemoryCompanyRepository();
        var legalRepo = new InMemoryLegalProfileRepository();
        var contractRepo = new InMemoryContractRepository();
        var rateRepo = new InMemoryCommissionRateRepository();
        var cache = new SpyCachePort();

        var company = Company.Create(CompanyId.From(Guid.NewGuid()), "Test Co", "test-co", "d", null, "mail", "phone", Address.Empty, JsonBlob.Empty);
        companyRepo.Add(company);
        legalRepo.Add(CompanyLegalProfile.Create(
            CompanyLegalProfileId.From(1),
            company.Id,
            "Legal",
            CompanyLegalType.Llc,
            "12345678",
            null,
            null,
            false,
            8m));

        var sut = new ApproveCompanyCommandHandler(companyRepo, legalRepo, contractRepo, rateRepo, cache);
        var result = await sut.Handle(new ApproveCompanyCommand(company.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(companyRepo.Get(company.Id)!.IsApproved);
        Assert.NotNull(await contractRepo.GetActiveByCompanyIdAsync(company.Id, CancellationToken.None));
        Assert.NotNull(await rateRepo.GetActiveByCompanyIdAsync(company.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SetCommissionRate_Fails_For_NotApproved_Company()
    {
        var companyRepo = new InMemoryCompanyRepository();
        var contractRepo = new InMemoryContractRepository();
        var rateRepo = new InMemoryCommissionRateRepository();
        var cache = new SpyCachePort();

        var company = Company.Create(CompanyId.From(Guid.NewGuid()), "Test Co", "test-co-2", "d", null, "mail", "phone", Address.Empty, JsonBlob.Empty);
        companyRepo.Add(company);

        var sut = new SetCompanyCommissionRateCommandHandler(companyRepo, contractRepo, rateRepo, cache);
        var result = await sut.Handle(
            new SetCompanyCommissionRateCommand(company.Id.Value, 12m, DateTime.UtcNow.AddDays(1), "test", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not approved", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevokeCompanyApproval_Sets_Company_NotApproved()
    {
        var companyRepo = new InMemoryCompanyRepository();
        var cache = new SpyCachePort();
        var company = Company.Create(CompanyId.From(Guid.NewGuid()), "Test Co", "test-co-3", "d", null, "mail", "phone", Address.Empty, JsonBlob.Empty);
        company.Approve(Guid.NewGuid().ToString());
        companyRepo.Add(company);

        var sut = new RevokeCompanyApprovalCommandHandler(companyRepo, cache);
        var result = await sut.Handle(new RevokeCompanyApprovalCommand(company.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(companyRepo.Get(company.Id)!.IsApproved);
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private readonly Dictionary<Guid, Company> _items = new();
        public void Add(Company company) => _items[company.Id.Value] = company;
        public Company? Get(CompanyId id) => _items.GetValueOrDefault(id.Value);

        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct = default) => Task.FromResult(Get(id));
        public Task<Company?> GetApprovedNotDeletedBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.Slug == slug && x.IsApproved && !x.IsDeleted));
        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.ToList());
        public Task<IReadOnlyList<Company>> GetApprovedAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.Where(x => x.IsApproved && !x.IsDeleted).ToList());
        public Task<IReadOnlyList<Company>> GetPendingApprovalAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(_items.Values.Where(x => !x.IsApproved && !x.IsDeleted).ToList());
        public Task AddAsync(Company company, CancellationToken ct = default) { Add(company); return Task.CompletedTask; }
        public Task UpdateAsync(Company company, CancellationToken ct = default) { Add(company); return Task.CompletedTask; }
    }

    private sealed class InMemoryLegalProfileRepository : ICompanyLegalProfileRepository
    {
        private readonly Dictionary<Guid, CompanyLegalProfile> _items = new();
        public void Add(CompanyLegalProfile profile) => _items[profile.CompanyId.Value] = profile;
        public Task<CompanyLegalProfile?> GetByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(companyId.Value));
        public Task AddAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default) { Add(legalProfile); return Task.CompletedTask; }
        public Task UpdateAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default) { Add(legalProfile); return Task.CompletedTask; }
    }

    private sealed class InMemoryContractRepository : ICompanyContractRepository
    {
        private readonly Dictionary<long, CompanyContract> _items = new();
        public Task<CompanyContract?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.CompanyId.Value == companyId.Value));
        public Task<CompanyContract?> GetByIdAsync(CompanyContractId id, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.Id == id));
        public Task<CompanyContract> AddAsync(CompanyContract contract, CancellationToken ct = default)
        {
            var stored = CompanyContract.Reconstitute(
                CompanyContractId.From(_items.Count + 1),
                contract.CompanyId,
                contract.ContractNumber,
                contract.Status,
                contract.EffectiveFrom,
                contract.EffectiveTo,
                contract.SignedAt,
                contract.Notes,
                contract.CreatedAt,
                contract.UpdatedAt,
                contract.IsDeleted,
                contract.DeletedAt);
            _items[stored.Id.Value] = stored;
            return Task.FromResult(stored);
        }
    }

    private sealed class InMemoryCommissionRateRepository : ICompanyCommissionRateRepository
    {
        private readonly Dictionary<Guid, CompanyCommissionRate> _activeByCompany = new();

        public Task<CompanyCommissionRate?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_activeByCompany.GetValueOrDefault(companyId.Value));

        public Task<CompanyCommissionRate> AddAsync(CompanyCommissionRate rate, CancellationToken ct = default)
        {
            _activeByCompany[rate.CompanyId.Value] = rate;
            return Task.FromResult(rate);
        }

        public Task UpdateAsync(CompanyCommissionRate rate, CancellationToken ct = default)
        {
            _activeByCompany[rate.CompanyId.Value] = rate;
            return Task.CompletedTask;
        }
    }
}
