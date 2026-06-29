using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using Marketplace.Application.Common.Options;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Favorites;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Favorites")]
[Trait("Layer", "IntegrationContainers")]
public sealed class FavoritesPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public FavoritesPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Add_List_Remove_Favorites_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = new NoopCachePort();
        await SeedProductAsync(db, 77001);

        var favoriteRepo = new FavoriteRepository(db);
        var productRepo = new ProductRepository(db);
        var add = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, cache);
        var list = new GetMyFavoritesQueryHandler(favoriteRepo, cache, Options.Create(new CacheTtlOptions()));
        var remove = new RemoveFavoriteProductCommandHandler(favoriteRepo, cache);
        var userId = Guid.NewGuid();

        Assert.True((await add.Handle(new AddFavoriteProductCommand(userId, 77001), CancellationToken.None)).IsSuccess);
        var listed = await list.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);

        Assert.True((await remove.Handle(new RemoveFavoriteProductCommand(userId, 77001), CancellationToken.None)).IsSuccess);
        var afterRemove = await list.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);
        Assert.Empty(afterRemove.Value!);
    }

    private static async Task SeedProductAsync(ApplicationDbContext db, long productId)
    {
        var now = DateTime.UtcNow;
        await new ProductRepository(db).AddAsync(Product.Reconstitute(
            ProductId.From(productId), CompanyId.From(Guid.NewGuid()), "Fav Product", $"fav-{productId}", "d",
            new Money(50), null, 1, 0, CategoryId.From(1), ProductStatus.Active, null, 0, 0, 0, false, now, now, false, null), CancellationToken.None);
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }
}
