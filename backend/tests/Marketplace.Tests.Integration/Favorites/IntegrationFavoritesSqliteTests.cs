using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Favorites")]
public class IntegrationFavoritesSqliteTests
{
    [Fact]
    public async Task Add_List_Remove_Flow_Works_With_Real_Db()
    {
        await using var db = await CreateSqliteContextAsync();
        var cache = new SpyCachePort();
        await SeedProductAsync(db, 7001, ProductStatus.Active);

        var favoriteRepo = new FavoriteRepository(db);
        var productRepo = new ProductRepository(db);
        var add = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, cache);
        var list = new GetMyFavoritesQueryHandler(favoriteRepo, cache, Options.Create(new CacheTtlOptions()));
        var remove = new RemoveFavoriteProductCommandHandler(favoriteRepo, cache);
        var userId = Guid.NewGuid();

        var added = await add.Handle(new AddFavoriteProductCommand(userId, 7001), CancellationToken.None);
        var listed = await list.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);
        var removed = await remove.Handle(new RemoveFavoriteProductCommand(userId, 7001), CancellationToken.None);
        var listedAfterRemove = await list.Handle(new GetMyFavoritesQuery(userId), CancellationToken.None);

        Assert.True(added.IsSuccess);
        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);
        Assert.True(removed.IsSuccess);
        Assert.True(listedAfterRemove.IsSuccess);
        Assert.Empty(listedAfterRemove.Value!);
    }

    [Fact]
    public async Task Add_After_SoftDelete_Reactivates_Existing_Row()
    {
        await using var db = await CreateSqliteContextAsync();
        var cache = new SpyCachePort();
        await SeedProductAsync(db, 7002, ProductStatus.Active);

        var favoriteRepo = new FavoriteRepository(db);
        var productRepo = new ProductRepository(db);
        var add = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, cache);
        var remove = new RemoveFavoriteProductCommandHandler(favoriteRepo, cache);
        var userId = Guid.NewGuid();

        var first = await add.Handle(new AddFavoriteProductCommand(userId, 7002), CancellationToken.None);
        _ = await remove.Handle(new RemoveFavoriteProductCommand(userId, 7002), CancellationToken.None);
        var second = await add.Handle(new AddFavoriteProductCommand(userId, 7002), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);

        var allRows = await db.Favorites.IgnoreQueryFilters().Where(x => x.UserId == userId && x.ProductId == 7002).ToListAsync();
        Assert.Single(allRows);
        Assert.False(allRows[0].IsDeleted);
    }

    [Fact]
    public async Task Favorites_Are_Isolated_By_User()
    {
        await using var db = await CreateSqliteContextAsync();
        var cache = new SpyCachePort();
        await SeedProductAsync(db, 7003, ProductStatus.Active);

        var favoriteRepo = new FavoriteRepository(db);
        var productRepo = new ProductRepository(db);
        var add = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, cache);
        var list = new GetMyFavoritesQueryHandler(favoriteRepo, cache, Options.Create(new CacheTtlOptions()));
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _ = await add.Handle(new AddFavoriteProductCommand(userA, 7003), CancellationToken.None);
        var listA = await list.Handle(new GetMyFavoritesQuery(userA), CancellationToken.None);
        var listB = await list.Handle(new GetMyFavoritesQuery(userB), CancellationToken.None);

        Assert.True(listA.IsSuccess);
        Assert.Single(listA.Value!);
        Assert.True(listB.IsSuccess);
        Assert.Empty(listB.Value!);
    }

    [Fact]
    public async Task Favorites_Mutations_Invalidate_Cache_Key()
    {
        await using var db = await CreateSqliteContextAsync();
        var cache = new SpyCachePort();
        await SeedProductAsync(db, 7004, ProductStatus.Active);

        var favoriteRepo = new FavoriteRepository(db);
        var productRepo = new ProductRepository(db);
        var add = new AddFavoriteProductCommandHandler(favoriteRepo, productRepo, cache);
        var remove = new RemoveFavoriteProductCommandHandler(favoriteRepo, cache);
        var userId = Guid.NewGuid();

        _ = await add.Handle(new AddFavoriteProductCommand(userId, 7004), CancellationToken.None);
        _ = await remove.Handle(new RemoveFavoriteProductCommand(userId, 7004), CancellationToken.None);

        Assert.True(cache.RemovedKeys.Count(x => x == FavoritesCacheKeys.ListByUser(userId)) >= 2);
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

    private static async Task SeedProductAsync(ApplicationDbContext db, long id, ProductStatus status)
    {
        var productRepo = new ProductRepository(db);
        await productRepo.AddAsync(
            Product.Reconstitute(
                ProductId.From(id),
                CompanyId.From(Guid.NewGuid()),
                $"Product {id}",
                $"product-{id}",
                "desc",
                new Money(55m),
                null,
                5,
                1,
                CategoryId.From(1),
                status,
                null,
                0,
                0,
                0,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null),
            CancellationToken.None);
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        private readonly Dictionary<string, object> _items = new();
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_items.TryGetValue(key, out var value) ? value as T : null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _items[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            _items.Remove(key);
            return Task.CompletedTask;
        }
    }
}
