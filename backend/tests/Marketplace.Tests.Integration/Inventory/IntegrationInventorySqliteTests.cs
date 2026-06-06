using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.Commands.ReleaseReservation;
using Marketplace.Application.Inventory.Commands.ReserveStock;
using Marketplace.Application.Inventory.Commands.TransferStock;
using Marketplace.Application.Inventory.Services;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Inventory")]
public class IntegrationInventorySqliteTests
{
    [Fact]
    public async Task Reserve_Then_Release_Updates_Stock_And_Reservation_Status()
    {
        await using var db = await CreateSqliteContextAsync();
        var companyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        const long warehouseId = 200;
        const long productId = 300;
        const string reservationCode = "reserve-release-flow";

        await SeedProductAsync(db, companyId, productId);
        await new WarehouseStockRepository(db).AddAsync(
            WarehouseStock.Create(
                WarehouseStockId.From(0),
                CompanyId.From(companyId),
                WarehouseId.From(warehouseId),
                ProductId.From(productId),
                onHand: 10,
                reserved: 0,
                reorderPoint: 1),
            CancellationToken.None);

        var reserveHandler = new ReserveStockCommandHandler(
            new AllowInventoryAccessService(),
            new WarehouseStockRepository(db),
            new InventoryReservationRepository(db),
            new StockMovementRepository(db),
            new ProductRepository(db),
            new SpyCachePort(),
            new StubOutboxWriter(),
            new NoOpTransactionPort());

        var releaseHandler = new ReleaseReservationCommandHandler(
            new AllowInventoryAccessService(),
            new InventoryReservationRepository(db),
            new WarehouseStockRepository(db),
            new ProductRepository(db),
            new SpyCachePort(),
            new StubOutboxWriter(),
            new NoOpTransactionPort(),
            new SpyRestockNotifier());

        var reserve = await reserveHandler.Handle(
            new ReserveStockCommand(companyId, warehouseId, productId, 4, reservationCode, 20, "order-1", actorId, false),
            CancellationToken.None);
        var afterReserve = await new WarehouseStockRepository(db).GetByWarehouseAndProductAsync(
            WarehouseId.From(warehouseId),
            ProductId.From(productId),
            CancellationToken.None);

        var release = await releaseHandler.Handle(
            new ReleaseReservationCommand(companyId, reservationCode, actorId, false),
            CancellationToken.None);
        var afterRelease = await new WarehouseStockRepository(db).GetByWarehouseAndProductAsync(
            WarehouseId.From(warehouseId),
            ProductId.From(productId),
            CancellationToken.None);
        var reservation = await new InventoryReservationRepository(db).GetByCodeAsync(
            CompanyId.From(companyId),
            reservationCode,
            CancellationToken.None);

        Assert.True(reserve.IsSuccess);
        Assert.NotNull(afterReserve);
        Assert.Equal(4, afterReserve!.Reserved);
        Assert.True(release.IsSuccess);
        Assert.NotNull(afterRelease);
        Assert.Equal(0, afterRelease!.Reserved);
        Assert.NotNull(reservation);
        Assert.Equal(InventoryReservationStatus.Released, reservation!.Status);
    }

    [Fact]
    public async Task TransferStock_Moves_Units_Between_Warehouses_And_Writes_Movements()
    {
        await using var db = await CreateSqliteContextAsync();
        var companyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        const long productId = 301;
        const long sourceWarehouseId = 201;
        const long targetWarehouseId = 202;

        await SeedProductAsync(db, companyId, productId);
        var stockRepo = new WarehouseStockRepository(db);
        await stockRepo.AddAsync(
            WarehouseStock.Create(
                WarehouseStockId.From(0),
                CompanyId.From(companyId),
                WarehouseId.From(sourceWarehouseId),
                ProductId.From(productId),
                onHand: 9,
                reserved: 0,
                reorderPoint: 1),
            CancellationToken.None);
        await stockRepo.AddAsync(
            WarehouseStock.Create(
                WarehouseStockId.From(0),
                CompanyId.From(companyId),
                WarehouseId.From(targetWarehouseId),
                ProductId.From(productId),
                onHand: 0,
                reserved: 0,
                reorderPoint: 1),
            CancellationToken.None);

        var handler = new TransferStockCommandHandler(
            new AllowInventoryAccessService(),
            stockRepo,
            new StockMovementRepository(db),
            new ProductRepository(db),
            new SpyCachePort(),
            new NoOpTransactionPort(),
            new SpyRestockNotifier());

        var result = await handler.Handle(
            new TransferStockCommand(companyId, sourceWarehouseId, targetWarehouseId, productId, 3, "transfer-sqlite-1", actorId, false),
            CancellationToken.None);

        var source = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(sourceWarehouseId), ProductId.From(productId), CancellationToken.None);
        var target = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(targetWarehouseId), ProductId.From(productId), CancellationToken.None);
        var movements = await new StockMovementRepository(db).ListByCompanyAndProductAsync(
            CompanyId.From(companyId),
            ProductId.From(productId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(source);
        Assert.NotNull(target);
        Assert.Equal(6, source!.OnHand);
        Assert.Equal(3, target!.OnHand);
        Assert.Contains(movements, x => x.OperationId == "transfer-sqlite-1:out");
        Assert.Contains(movements, x => x.OperationId == "transfer-sqlite-1:in");
    }

    [Fact]
    public async Task ExpireReservations_Job_Releases_Expired_Reservations()
    {
        await using var db = await CreateSqliteContextAsync();
        var companyId = Guid.NewGuid();
        const long warehouseId = 203;
        const long productId = 302;
        const string reservationCode = "expired-res";

        var stockRepo = new WarehouseStockRepository(db);
        var reservationRepo = new InventoryReservationRepository(db);
        await stockRepo.AddAsync(
            WarehouseStock.Create(
                WarehouseStockId.From(0),
                CompanyId.From(companyId),
                WarehouseId.From(warehouseId),
                ProductId.From(productId),
                onHand: 7,
                reserved: 5,
                reorderPoint: 1),
            CancellationToken.None);
        await reservationRepo.AddAsync(
            InventoryReservation.Reconstitute(
                InventoryReservationId.From(1),
                CompanyId.From(companyId),
                WarehouseId.From(warehouseId),
                ProductId.From(productId),
                reservationCode,
                5,
                InventoryReservationStatus.Active,
                DateTime.UtcNow.AddMinutes(-5),
                "job-test",
                DateTime.UtcNow.AddMinutes(-15),
                DateTime.UtcNow.AddMinutes(-15),
                false,
                null),
            CancellationToken.None);

        var job = new InventoryJobs(reservationRepo, stockRepo);
        await job.ExpireReservationsAsync(CancellationToken.None);

        var stock = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(warehouseId), ProductId.From(productId), CancellationToken.None);
        var reservation = await reservationRepo.GetByCodeAsync(CompanyId.From(companyId), reservationCode, CancellationToken.None);

        Assert.NotNull(stock);
        Assert.NotNull(reservation);
        Assert.Equal(0, stock!.Reserved);
        Assert.Equal(InventoryReservationStatus.Expired, reservation!.Status);
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

    private static async Task SeedProductAsync(ApplicationDbContext db, Guid companyId, long productId)
    {
        await new ProductRepository(db).AddAsync(
            Product.Reconstitute(
                ProductId.From(productId),
                CompanyId.From(companyId),
                $"Product {productId}",
                $"product-{productId}",
                "desc",
                new Money(50m),
                null,
                3,
                1,
                CategoryId.From(1),
                ProductStatus.Active,
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

    private sealed class AllowInventoryAccessService : IInventoryAccessService
    {
        public Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, InventoryPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class NoOpTransactionPort : IAppTransactionPort
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
            => action(ct);
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class SpyRestockNotifier : IRestockAvailabilityNotifier
    {
        public Task NotifyIfCrossedFromZeroAsync(Guid companyId, long productId, int beforeAvailableSum, int afterAvailableSum, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class StubOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
