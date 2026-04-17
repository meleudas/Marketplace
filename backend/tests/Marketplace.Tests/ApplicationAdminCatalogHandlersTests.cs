using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.Commands.DeactivateCategory;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Tests;

public class ApplicationAdminCatalogHandlersTests
{
    [Fact]
    public async Task CreateCompanyHandler_Creates_Company()
    {
        var repo = new InMemoryCompanyRepository();
        var handler = new CreateCompanyCommandHandler(repo, new NoOpCachePort());
        var command = new CreateCompanyCommand(
            "Company",
            "company",
            "Description",
            null,
            "mail@company.com",
            "+380000000000",
            new CompanyAddressDto("Street", "City", "State", "00000", "UA"),
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task ApproveCompanyHandler_Returns_Failure_When_Not_Found()
    {
        var repo = new InMemoryCompanyRepository();
        var handler = new ApproveCompanyCommandHandler(repo, new NoOpCachePort());

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
    public async Task DeactivateCategoryHandler_Changes_State()
    {
        var repo = new InMemoryCategoryRepository();
        var category = Category.Create(CategoryId.From(7), "Cat", "cat", null, null, null, JsonBlob.Empty, 0, true);
        repo.Items[category.Id.Value] = category;
        var handler = new DeactivateCategoryCommandHandler(repo, new NoOpCachePort());

        var result = await handler.Handle(new DeactivateCategoryCommand(7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(repo.Items[7].IsActive);
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

    private sealed class NoOpCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
