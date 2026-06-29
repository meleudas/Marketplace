using Marketplace.Application.Behavior.Ports;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Recommendations;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Recommendations")]
[Trait("Layer", "IntegrationContainers")]
public sealed class RecommendationPipelineContainersTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public RecommendationPipelineContainersTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Train_Promote_And_Serve_Personalized_Recommendations()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var warehouse = scope.ServiceProvider.GetRequiredService<IAnalyticsWarehouseWriter>();
        var aggregationJobs = scope.ServiceProvider.GetRequiredService<AnalyticsWarehouseAggregationJobs>();
        var modelJobs = scope.ServiceProvider.GetRequiredService<RecommendationModelJobs>();
        var loader = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.External.Recommendations.RecommendationModelLoader>();
        var recoService = scope.ServiceProvider.GetRequiredService<IPersonalizedRecommendationService>();

        var userId = Guid.NewGuid();
        const long productId = 91001;
        await SeedProductAsync(db, productId);

        var now = DateTime.UtcNow;
        for (var i = 0; i < 3; i++)
        {
            await warehouse.WriteEventAsync(new AnalyticsWarehouseEvent(
                Guid.NewGuid(),
                i % 2 == 0 ? "ProductView" : "FavoriteAdd",
                now.AddMinutes(-i),
                userId,
                $"sess-{i}",
                productId,
                null,
                "catalog",
                1,
                "{}",
                now), CancellationToken.None);
        }

        await aggregationJobs.RebuildUserItemSignalsAsync(CancellationToken.None);
        await modelJobs.TrainAndValidateAsync(CancellationToken.None);
        await modelJobs.PromoteCandidateAsync(CancellationToken.None);
        await loader.InvalidateAsync();

        var active = await loader.GetActiveAsync(CancellationToken.None);
        Assert.NotNull(active);

        var result = await recoService.GetForUserAsync(userId, 5, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.UsedFallback);
        Assert.NotEmpty(result.Value.Items);
    }

    [Fact]
    public async Task Inference_Falls_Back_When_No_Active_Model()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<Marketplace.Application.Common.Ports.IObjectStorage>();
        var loader = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.External.Recommendations.RecommendationModelLoader>();
        foreach (var key in await storage.ListKeysAsync("ml/recommendations/", CancellationToken.None))
            await storage.DeleteAsync(key, CancellationToken.None);
        await loader.InvalidateAsync();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recoService = scope.ServiceProvider.GetRequiredService<IPersonalizedRecommendationService>();
        await SeedProductAsync(db, 91002);

        var result = await recoService.GetForUserAsync(Guid.NewGuid(), 5, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.UsedFallback);
        Assert.Equal("fallback", result.Value.ModelVersion);
    }

    private static async Task SeedProductAsync(ApplicationDbContext db, long productId)
    {
        var now = DateTime.UtcNow;
        var companyId = Guid.NewGuid();
        const long categoryId = 9100;

        if (!await db.Categories.AsNoTracking().AnyAsync(x => x.Id == categoryId))
        {
            db.Categories.Add(new Marketplace.Infrastructure.Persistence.Entities.CategoryRecord
            {
                Id = categoryId,
                Name = "Reco",
                Slug = "reco",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var products = new ProductRepository(db);
        await products.AddAsync(
            Product.Reconstitute(
                ProductId.From(productId),
                CompanyId.From(companyId),
                $"Reco Product {productId}",
                $"reco-product-{productId}",
                "desc",
                new Money(100),
                null,
                1,
                0,
                CategoryId.From(categoryId),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                now,
                now,
                false,
                null),
            CancellationToken.None);
    }
}
