using Elastic.Clients.Elasticsearch;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.CreateProduct;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.External.Search;
using Marketplace.Infrastructure.External.Search.Documents;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Catalog")]
[Trait("Layer", "IntegrationContainers")]
public sealed class AdminCatalogReindexContainersTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public AdminCatalogReindexContainersTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Approve_Product_Reindexes_Document_In_Elasticsearch()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();
        var indexManager = scope.ServiceProvider.GetRequiredService<ProductSearchIndexManager>();
        var companyId = Guid.NewGuid();
        const long categoryId = 601;
        var now = DateTime.UtcNow;

        db.Categories.Add(new Marketplace.Infrastructure.Persistence.Entities.CategoryRecord
        {
            Id = categoryId,
            Name = "Admin Cat",
            Slug = "admin-cat",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var create = new CreateProductCommandHandler(
            new AllowAccessService(),
            new ProductRepository(db),
            new ProductDetailRepository(db),
            new ProductImageRepository(db),
            new NoopCachePort(),
            new NoopAppNotifications());
        var created = await create.Handle(
            new CreateProductCommand(
                companyId,
                Guid.NewGuid(),
                false,
                "Container Keyboard",
                "container-keyboard",
                "desc",
                200,
                null,
                0,
                categoryId,
                false,
                null,
                null),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var approve = new ApproveProductCommandHandler(
            new ProductRepository(db),
            new NoopCachePort(),
            scope.ServiceProvider.GetRequiredService<IProductSearchIndexDispatcher>(),
            new NoopAppNotifications());
        var approved = await approve.Handle(new ApproveProductCommand(created.Value!.Product.Id, Guid.NewGuid()), CancellationToken.None);
        Assert.True(approved.IsSuccess);

        var searchJobs = scope.ServiceProvider.GetRequiredService<SearchIndexJobs>();
        await searchJobs.UpsertProductAsync(created.Value.Product.Id, CancellationToken.None);

        await indexManager.EnsureIndexExistsAsync();
        var response = await client.GetAsync<ProductSearchDocument>(
            indexManager.IndexName,
            created.Value.Product.Id,
            CancellationToken.None);
        Assert.True(response.Found);
        Assert.Equal("active", response.Source!.Status);
    }

    private sealed class AllowAccessService : IProductAccessService
    {
        public Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, ProductPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopAppNotifications : IAppNotificationScheduler
    {
        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
    }
}
