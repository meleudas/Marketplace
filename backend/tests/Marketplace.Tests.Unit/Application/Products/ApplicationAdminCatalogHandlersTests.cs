using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.Commands.DeleteCategory;
using Marketplace.Application.Categories.Commands.DeactivateCategory;
using Marketplace.Application.Categories.Commands.UpdateCategory;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public class ApplicationAdminCatalogHandlersTests
{
    [Fact]
    public async Task CreateCompanyHandler_Creates_Company()
    {
        var repo = new InMemoryCompanyRepository();
        var legalRepo = new InMemoryCompanyLegalProfileRepository();
        var handler = new CreateCompanyCommandHandler(repo, legalRepo, new NoOpCachePort());
        var command = new CreateCompanyCommand(
            "Company",
            "company",
            "Description",
            null,
            "mail@company.com",
            "+380000000000",
            new CompanyAddressDto("Street", "City", "State", "00000", "UA"),
            new CompanyLegalProfileDto("Company LLC", "llc", "12345678", null, null, true, 12m),
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        Assert.Single(legalRepo.Items);
    }

    [Fact]
    public async Task ApproveCompanyHandler_Returns_Failure_When_Not_Found()
    {
        var repo = new InMemoryCompanyRepository();
        var handler = new ApproveCompanyCommandHandler(
            repo,
            new InMemoryCompanyLegalProfileRepository(),
            new InMemoryCompanyContractRepository(),
            new InMemoryCompanyCommissionRateRepository(),
            new NoOpCachePort());

        var result = await handler.Handle(new ApproveCompanyCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCategoryHandler_Creates_Category()
    {
        var repo = new InMemoryCategoryRepository();
        var handler = new CreateCategoryCommandHandler(repo, new NoOpCachePort());

        var result = await handler.Handle(
            new CreateCategoryCommand("Cat", "cat", null, null, null, null, 0, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
    }

    [Fact]
    [Trait("Suite", "CatalogCategories")]
    public async Task CreateCategory_Fails_When_Parent_Not_Found()
    {
        var repo = new InMemoryCategoryRepository();
        var handler = new CreateCategoryCommandHandler(repo, new NoOpCachePort());

        var result = await handler.Handle(
            new CreateCategoryCommand("Cat", "cat", null, 999, null, null, 0, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("parent category not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeactivateCategoryHandler_Changes_State()
    {
        var repo = new InMemoryCategoryRepository();
        var category = Category.Create(CategoryId.From(7), "Cat", "cat", null, null, null, JsonBlob.Empty, 0, true);
        repo.Items[category.Id.Value] = category;
        var cache = new SpyCachePort();
        var handler = new DeactivateCategoryCommandHandler(repo, cache);

        var result = await handler.Handle(new DeactivateCategoryCommand(7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(repo.Items[7].IsActive);
        Assert.Contains(CatalogCacheKeys.ProductList, cache.RemovedKeys);
    }

    [Fact]
    [Trait("Suite", "CatalogCategories")]
    public async Task UpdateCategory_Fails_When_Parent_Is_Self()
    {
        var repo = new InMemoryCategoryRepository();
        var category = Category.Create(CategoryId.From(5), "Cat", "cat", null, null, null, JsonBlob.Empty, 0, true);
        repo.Items[category.Id.Value] = category;
        var handler = new UpdateCategoryCommandHandler(repo, new NoOpCachePort());

        var result = await handler.Handle(
            new UpdateCategoryCommand(5, "Cat", "cat", null, 5, null, null, 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("own parent", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Suite", "CatalogCategories")]
    public async Task DeleteCategory_Fails_When_Has_Child_Category()
    {
        var repo = new InMemoryCategoryRepository();
        var parent = Category.Create(CategoryId.From(10), "Parent", "parent", null, null, null, JsonBlob.Empty, 0, true);
        var child = Category.Create(CategoryId.From(11), "Child", "child", null, CategoryId.From(10), null, JsonBlob.Empty, 0, true);
        repo.Items[parent.Id.Value] = parent;
        repo.Items[child.Id.Value] = child;
        var handler = new DeleteCategoryCommandHandler(repo, new InMemoryProductRepository(), new NoOpCachePort());

        var result = await handler.Handle(new DeleteCategoryCommand(10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("child categories", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Suite", "CatalogCategories")]
    public async Task DeleteCategory_Fails_When_Has_Active_Products()
    {
        var repo = new InMemoryCategoryRepository();
        var category = Category.Create(CategoryId.From(20), "Parent", "parent", null, null, null, JsonBlob.Empty, 0, true);
        repo.Items[category.Id.Value] = category;
        var products = new InMemoryProductRepository();
        products.Seed(Product.Reconstitute(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "P",
            "p",
            "d",
            new Money(10),
            null,
            3,
            0,
            CategoryId.From(20),
            ProductStatus.Active,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null));
        var handler = new DeleteCategoryCommandHandler(repo, products, new NoOpCachePort());

        var result = await handler.Handle(new DeleteCategoryCommand(20), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("active products", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        public Dictionary<Guid, Company> Items { get; } = new();

        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct = default)
            => Task.FromResult(Items.GetValueOrDefault(id.Value));

        public Task<Company?> GetApprovedNotDeletedBySlugAsync(string slug, CancellationToken ct = default)
        {
            var match = Items.Values.FirstOrDefault(c =>
                string.Equals(c.Slug, slug.Trim(), StringComparison.Ordinal) && c.IsApproved && !c.IsDeleted);
            return Task.FromResult(match);
        }

        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(Items.Values.ToList());

        public Task<IReadOnlyList<Company>> GetApprovedAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(Items.Values.Where(x => x.IsApproved && !x.IsDeleted).ToList());

        public Task<IReadOnlyList<Company>> GetPendingApprovalAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Company>>(Items.Values.Where(x => !x.IsApproved).ToList());

        public Task AddAsync(Company company, CancellationToken ct = default)
        {
            Items[company.Id.Value] = company;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Company company, CancellationToken ct = default)
        {
            Items[company.Id.Value] = company;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        public Dictionary<long, Category> Items { get; } = new();
        private long _nextId = 1;

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
            => Task.FromResult(Items.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Category>>(Items.Values.ToList());

        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Category>>(Items.Values.Where(x => x.IsActive).ToList());

        public Task<Category> AddAsync(Category category, CancellationToken ct = default)
        {
            if (category.Id.Value == 0)
            {
                category = Category.Reconstitute(
                    CategoryId.From(_nextId++),
                    category.Name,
                    category.Slug,
                    category.ImageUrl,
                    category.ParentId,
                    category.Description,
                    category.Meta,
                    category.SortOrder,
                    category.IsActive,
                    category.ProductCount,
                    category.CreatedAt,
                    category.UpdatedAt,
                    category.IsDeleted,
                    category.DeletedAt);
            }

            Items[category.Id.Value] = category;
            return Task.FromResult(category);
        }

        public Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            Items[category.Id.Value] = category;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = new();

        public void Seed(Product product) => _items[product.Id.Value] = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.CompanyId == companyId && x.Slug == slug));

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(_items.Values.FirstOrDefault(x => x.Slug == slug));

        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
        {
            var set = ids.Select(x => x.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => set.Contains(x.Id.Value)).ToList());
        }

        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.CompanyId == companyId).ToList());

        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.Active && !x.IsDeleted).ToList());

        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.PendingReview && !x.IsDeleted).ToList());

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            Seed(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default)
        {
            Seed(product);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCompanyLegalProfileRepository : ICompanyLegalProfileRepository
    {
        public Dictionary<Guid, CompanyLegalProfile> Items { get; } = new();
        private long _nextId = 1;

        public Task<CompanyLegalProfile?> GetByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(Items.GetValueOrDefault(companyId.Value));

        public Task AddAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default)
        {
            if (legalProfile.Id.Value == 0)
            {
                legalProfile = CompanyLegalProfile.Reconstitute(
                    CompanyLegalProfileId.From(_nextId++),
                    legalProfile.CompanyId,
                    legalProfile.LegalName,
                    legalProfile.LegalType,
                    legalProfile.Edrpou,
                    legalProfile.Ipn,
                    legalProfile.CertificateNumber,
                    legalProfile.IsVatPayer,
                    legalProfile.InitialCommissionPercent,
                    legalProfile.PayoutIban,
                    legalProfile.PayoutRecipientName,
                    legalProfile.PayoutProviderAccountId,
                    legalProfile.CreatedAt,
                    legalProfile.UpdatedAt,
                    legalProfile.IsDeleted,
                    legalProfile.DeletedAt);
            }

            Items[legalProfile.CompanyId.Value] = legalProfile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default)
        {
            Items[legalProfile.CompanyId.Value] = legalProfile;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCompanyContractRepository : ICompanyContractRepository
    {
        private readonly Dictionary<Guid, CompanyContract> _contractsByCompany = new();
        private long _nextId = 1;

        public Task<CompanyContract?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_contractsByCompany.GetValueOrDefault(companyId.Value));

        public Task<CompanyContract?> GetByIdAsync(CompanyContractId id, CancellationToken ct = default)
            => Task.FromResult(_contractsByCompany.Values.FirstOrDefault(x => x.Id == id));

        public Task<CompanyContract> AddAsync(CompanyContract contract, CancellationToken ct = default)
        {
            if (contract.Id.Value == 0)
            {
                contract = CompanyContract.Reconstitute(
                    CompanyContractId.From(_nextId++),
                    contract.CompanyId,
                    contract.ContractNumber,
                    CompanyContractStatus.Active,
                    contract.EffectiveFrom,
                    contract.EffectiveTo,
                    contract.SignedAt,
                    contract.Notes,
                    contract.CreatedAt,
                    contract.UpdatedAt,
                    contract.IsDeleted,
                    contract.DeletedAt);
            }

            _contractsByCompany[contract.CompanyId.Value] = contract;
            return Task.FromResult(contract);
        }
    }

    private sealed class InMemoryCompanyCommissionRateRepository : ICompanyCommissionRateRepository
    {
        private readonly Dictionary<Guid, CompanyCommissionRate> _activeByCompany = new();
        private long _nextId = 1;

        public Task<CompanyCommissionRate?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult(_activeByCompany.GetValueOrDefault(companyId.Value));

        public Task<CompanyCommissionRate> AddAsync(CompanyCommissionRate rate, CancellationToken ct = default)
        {
            if (rate.Id.Value == 0)
            {
                rate = CompanyCommissionRate.Reconstitute(
                    CompanyCommissionRateId.From(_nextId++),
                    rate.CompanyId,
                    rate.ContractId,
                    rate.CommissionPercent,
                    rate.EffectiveFrom,
                    rate.EffectiveTo,
                    rate.Reason,
                    rate.CreatedByUserId,
                    rate.CreatedAt,
                    rate.UpdatedAt,
                    rate.IsDeleted,
                    rate.DeletedAt);
            }

            _activeByCompany[rate.CompanyId.Value] = rate;
            return Task.FromResult(rate);
        }

        public Task UpdateAsync(CompanyCommissionRate rate, CancellationToken ct = default)
        {
            _activeByCompany[rate.CompanyId.Value] = rate;
            return Task.CompletedTask;
        }

        public Task<CompanyCommissionRate?> GetActiveAtAsync(CompanyId companyId, DateTime asOfUtc, CancellationToken ct = default)
            => GetActiveByCompanyIdAsync(companyId, ct);

        public Task<IReadOnlyList<CompanyCommissionRate>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanyCommissionRate>>(
                _activeByCompany.TryGetValue(companyId.Value, out var rate) ? [rate] : []);
    }

    private sealed class NoOpCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
