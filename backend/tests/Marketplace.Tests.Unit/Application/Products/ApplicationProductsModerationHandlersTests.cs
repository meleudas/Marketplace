using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.CreateProduct;
using Marketplace.Application.Products.Commands.RejectProduct;
using Marketplace.Application.Products.Commands.UpdateProduct;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests;

[Trait("Suite", "ProductsModeration")]
public sealed class ApplicationProductsModerationHandlersTests
{
    [Fact]
    public async Task Approve_Returns_NotFound_When_Product_Does_Not_Exist()
    {
        var products = new InMemoryProductRepository();
        var handler = new ApproveProductCommandHandler(
            products,
            new SpyCachePort(),
            new SpySearchIndexDispatcher(),
            new SpyAppNotificationScheduler());

        var result = await handler.Handle(new ApproveProductCommand(404, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found", result.Error);
    }

    [Fact]
    public async Task Approve_Pending_Product_Triggers_Side_Effects()
    {
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        var search = new SpySearchIndexDispatcher();
        var notifications = new SpyAppNotificationScheduler();
        var product = BuildPendingProduct();
        products.Seed(product);
        var handler = new ApproveProductCommandHandler(products, cache, search, notifications);

        var result = await handler.Handle(new ApproveProductCommand(product.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await products.GetByIdAsync(product.Id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(ProductStatus.Active, saved!.Status);
        Assert.Contains(CatalogCacheKeys.ProductList, cache.RemovedKeys);
        Assert.Contains(CatalogCacheKeys.ProductDetailPrefix + product.Slug, cache.RemovedKeys);
        Assert.Contains(product.Id.Value, search.UpsertedIds);
        Assert.Single(notifications.Requests);
        Assert.Equal(AppNotificationCorrelationIds.ProductApprovedForUser(product.Id.Value, product.SubmittedByUserId!.Value), notifications.Requests[0].CorrelationId);
    }

    [Fact]
    public async Task Reject_From_Active_Fails_Without_Side_Effects()
    {
        var products = new InMemoryProductRepository();
        var cache = new SpyCachePort();
        var notifications = new SpyAppNotificationScheduler();
        var active = Product.Reconstitute(
            ProductId.From(7),
            CompanyId.From(Guid.NewGuid()),
            "Active",
            "active",
            "d",
            new Money(10),
            null,
            0,
            0,
            CategoryId.From(2),
            ProductStatus.Active,
            null,
            0,
            0,
            0,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null,
            Guid.NewGuid(),
            null);
        products.Seed(active);
        var handler = new RejectProductCommandHandler(products, cache, notifications);

        var result = await handler.Handle(new RejectProductCommand(active.Id.Value, Guid.NewGuid(), "bad"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Only products pending review can be rejected", result.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(cache.RemovedKeys);
        Assert.Empty(notifications.Requests);
    }

    [Fact]
    public async Task Create_Product_Submits_For_Moderation_And_Notifies_Admins()
    {
        var products = new InMemoryProductRepository();
        var notifications = new SpyAppNotificationScheduler();
        var handler = new CreateProductCommandHandler(
            new AllowAccessService(),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            new SpyCachePort(),
            notifications);

        var result = await handler.Handle(
            new CreateProductCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                false,
                "Name",
                "name-slug",
                "Desc",
                120,
                null,
                1,
                10,
                false,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("PendingReview", result.Value!.Product.Status);
        Assert.Single(notifications.Requests);
        Assert.Equal(
            AppNotificationCorrelationIds.ProductPendingReviewQueue(result.Value.Product.Id),
            notifications.Requests[0].CorrelationId);
    }

    [Fact]
    public async Task Update_Draft_Product_Resubmits_With_Deterministic_CorrelationId()
    {
        var products = new InMemoryProductRepository();
        var product = Product.Create(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "Draft",
            "draft-slug",
            "Desc",
            new Money(80),
            null,
            0,
            0,
            CategoryId.From(1),
            false);
        products.Seed(product);
        var notifications = new SpyAppNotificationScheduler();
        var handler = new UpdateProductCommandHandler(
            new AllowAccessService(),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            new SpyObjectStorage(),
            new SpyCachePort(),
            new SpySearchIndexDispatcher(),
            notifications);

        var result = await handler.Handle(
            new UpdateProductCommand(
                product.CompanyId.Value,
                product.Id.Value,
                Guid.NewGuid(),
                false,
                "Updated",
                "updated-slug",
                "Updated desc",
                100,
                null,
                1,
                2,
                false,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await products.GetByIdAsync(product.Id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(ProductStatus.PendingReview, saved!.Status);
        Assert.Single(notifications.Requests);
        Assert.Equal(
            AppNotificationCorrelationIds.ProductPendingReviewQueue(product.Id.Value),
            notifications.Requests[0].CorrelationId);
    }

    [Fact]
    public async Task Update_Returns_Product_Not_Found_For_Cross_Tenant()
    {
        var products = new InMemoryProductRepository();
        var product = BuildPendingProduct();
        products.Seed(product);
        var handler = new UpdateProductCommandHandler(
            new AllowAccessService(),
            products,
            new InMemoryProductDetailRepository(),
            new InMemoryProductImageRepository(),
            new SpyObjectStorage(),
            new SpyCachePort(),
            new SpySearchIndexDispatcher(),
            new SpyAppNotificationScheduler());

        var result = await handler.Handle(
            new UpdateProductCommand(
                Guid.NewGuid(),
                product.Id.Value,
                Guid.NewGuid(),
                false,
                "Updated",
                "updated",
                "d",
                100,
                null,
                1,
                2,
                false,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found", result.Error);
    }

    private static Product BuildPendingProduct()
    {
        var p = Product.Create(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "Pending",
            "pending-slug",
            "Desc",
            new Money(90),
            null,
            0,
            0,
            CategoryId.From(1),
            false);
        p.SubmitForModeration(Guid.NewGuid());
        return p;
    }

    private sealed class AllowAccessService : IProductAccessService
    {
        public Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, ProductPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }
    }

    private sealed class SpySearchIndexDispatcher : IProductSearchIndexDispatcher
    {
        public List<long> UpsertedIds { get; } = [];

        public Task EnqueueUpsertProductAsync(long productId, CancellationToken ct = default)
        {
            UpsertedIds.Add(productId);
            return Task.CompletedTask;
        }

        public Task EnqueueDeleteProductAsync(long productId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SpyAppNotificationScheduler : IAppNotificationScheduler
    {
        public List<AppNotificationRequest> Requests { get; } = [];

        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class SpyObjectStorage : IObjectStorage
    {
        public Task EnsureBucketExistsAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteAsync(string objectKey, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public string GetPublicUrl(string objectKey) => objectKey;
        public Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default) => Task.FromResult(objectKey);
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _items = [];
        private long _seq = 100;

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
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.Active).ToList());

        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(x => x.Status == ProductStatus.PendingReview).ToList());

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            var created = Product.Reconstitute(
                ProductId.From(++_seq),
                product.CompanyId,
                product.Name,
                product.Slug,
                product.Description,
                product.Price,
                product.OldPrice,
                product.Stock,
                product.MinStock,
                product.CategoryId,
                product.Status,
                product.Rating,
                product.ReviewCount,
                product.ViewCount,
                product.SalesCount,
                product.HasVariants,
                product.CreatedAt,
                product.UpdatedAt,
                product.IsDeleted,
                product.DeletedAt,
                product.SubmittedByUserId,
                product.ModerationRejectionReason);
            _items[created.Id.Value] = created;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductDetailRepository : IProductDetailRepository
    {
        public Task<ProductDetail?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default) => Task.FromResult<ProductDetail?>(null);
        public Task AddAsync(ProductDetail detail, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(ProductDetail detail, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductImageRepository : IProductImageRepository
    {
        public Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ProductImage>>([]);

        public Task ReplaceForProductAsync(ProductId productId, IReadOnlyList<ProductImage> images, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
