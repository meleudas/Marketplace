using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.Commands.ReceiveStock;
using Marketplace.Application.Inventory.Commands.ReleaseReservation;
using Marketplace.Application.Inventory.Commands.ReserveStock;
using Marketplace.Application.Inventory.Commands.ShipStock;
using Marketplace.Application.Inventory.Commands.TransferStock;
using Marketplace.Application.Inventory.Services;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Inventory")]
public class ApplicationInventoryHandlersTests
{
    [Fact]
    public async Task ReserveStock_Idempotent_When_Active_Reservation_Exists()
    {
        var companyId = Guid.NewGuid();
        var reservationCode = "res-1";
        var stockRepo = new InMemoryWarehouseStockRepository();
        stockRepo.Seed(CreateStock(companyId, warehouseId: 10, productId: 100, onHand: 8, reserved: 0));
        var reservationRepo = new InMemoryReservationRepository();
        reservationRepo.Seed(CreateReservation(companyId, 10, 100, reservationCode, 2, InventoryReservationStatus.Active));
        var movementRepo = new InMemoryStockMovementRepository();

        var handler = new ReserveStockCommandHandler(
            new StubInventoryAccessService(true),
            stockRepo,
            reservationRepo,
            movementRepo,
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new StubOutboxWriter(),
            new NoOpTransactionPort());

        var result = await handler.Handle(
            new ReserveStockCommand(companyId, 10, 100, 2, reservationCode, 30, "cart-1", Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stock = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(10), ProductId.From(100), CancellationToken.None);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Reserved);
        Assert.Empty(movementRepo.Movements);
    }

    [Fact]
    public async Task ReserveStock_Fails_When_Quantity_Exceeds_Available()
    {
        var companyId = Guid.NewGuid();
        var stockRepo = new InMemoryWarehouseStockRepository();
        stockRepo.Seed(CreateStock(companyId, warehouseId: 11, productId: 101, onHand: 3, reserved: 0));

        var handler = new ReserveStockCommandHandler(
            new StubInventoryAccessService(true),
            stockRepo,
            new InMemoryReservationRepository(),
            new InMemoryStockMovementRepository(),
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new StubOutboxWriter(),
            new NoOpTransactionPort());

        var result = await handler.Handle(
            new ReserveStockCommand(companyId, 11, 101, 4, "res-oversell", 10, null, Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Cannot reserve more than available", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseReservation_Returns_Success_For_NonActive_Reservation()
    {
        var companyId = Guid.NewGuid();
        var reservationCode = "res-released";
        var stockRepo = new InMemoryWarehouseStockRepository();
        stockRepo.Seed(CreateStock(companyId, warehouseId: 12, productId: 102, onHand: 10, reserved: 3));
        var reservationRepo = new InMemoryReservationRepository();
        reservationRepo.Seed(CreateReservation(companyId, 12, 102, reservationCode, 3, InventoryReservationStatus.Released));

        var handler = new ReleaseReservationCommandHandler(
            new StubInventoryAccessService(true),
            reservationRepo,
            stockRepo,
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new StubOutboxWriter(),
            new NoOpTransactionPort(),
            new SpyRestockNotifier());

        var result = await handler.Handle(
            new ReleaseReservationCommand(companyId, reservationCode, Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stock = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(12), ProductId.From(102), CancellationToken.None);
        Assert.NotNull(stock);
        Assert.Equal(3, stock!.Reserved);
    }

    [Fact]
    public async Task ShipStock_Returns_AlreadyProcessed_For_Duplicate_OperationId()
    {
        var companyId = Guid.NewGuid();
        var movementRepo = new InMemoryStockMovementRepository();
        movementRepo.Seed(CreateMovement(companyId, 13, 103, StockMovementType.Outbound, 1, "ship-op-1"));

        var handler = new ShipStockCommandHandler(
            new StubInventoryAccessService(true),
            new InMemoryWarehouseStockRepository(),
            movementRepo,
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new NoOpTransactionPort());

        var result = await handler.Handle(
            new ShipStockCommand(companyId, 13, 103, 1, "ship-op-1", "order-1", Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Operation already processed", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReceiveStock_Forbidden_When_Access_Denied()
    {
        var handler = new ReceiveStockCommandHandler(
            new StubInventoryAccessService(false),
            new InMemoryWarehouseStockRepository(),
            new InMemoryStockMovementRepository(),
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new SpyRestockNotifier());

        var result = await handler.Handle(
            new ReceiveStockCommand(Guid.NewGuid(), 14, 104, 2, "recv-op-1", "po-1", Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task TransferStock_Moves_Units_And_Writes_Movements()
    {
        var companyId = Guid.NewGuid();
        var stockRepo = new InMemoryWarehouseStockRepository();
        stockRepo.Seed(CreateStock(companyId, warehouseId: 15, productId: 105, onHand: 10, reserved: 0));
        var movementRepo = new InMemoryStockMovementRepository();

        var handler = new TransferStockCommandHandler(
            new StubInventoryAccessService(true),
            stockRepo,
            movementRepo,
            new InMemoryProductRepository(),
            new SpyCachePort(),
            new NoOpTransactionPort(),
            new SpyRestockNotifier());

        var result = await handler.Handle(
            new TransferStockCommand(companyId, 15, 16, 105, 4, "transfer-op-1", Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var source = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(15), ProductId.From(105), CancellationToken.None);
        var target = await stockRepo.GetByWarehouseAndProductAsync(WarehouseId.From(16), ProductId.From(105), CancellationToken.None);
        Assert.NotNull(source);
        Assert.NotNull(target);
        Assert.Equal(6, source!.OnHand);
        Assert.Equal(4, target!.OnHand);
        Assert.Contains(movementRepo.Movements, x => x.OperationId == "transfer-op-1:out");
        Assert.Contains(movementRepo.Movements, x => x.OperationId == "transfer-op-1:in");
    }

    private static WarehouseStock CreateStock(Guid companyId, long warehouseId, long productId, int onHand, int reserved)
    {
        return WarehouseStock.Reconstitute(
            WarehouseStockId.From(warehouseId * 100 + productId),
            CompanyId.From(companyId),
            WarehouseId.From(warehouseId),
            ProductId.From(productId),
            onHand,
            reserved,
            0,
            1,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
    }

    private static InventoryReservation CreateReservation(
        Guid companyId,
        long warehouseId,
        long productId,
        string code,
        int quantity,
        InventoryReservationStatus status)
    {
        return InventoryReservation.Reconstitute(
            InventoryReservationId.From(1),
            CompanyId.From(companyId),
            WarehouseId.From(warehouseId),
            ProductId.From(productId),
            code,
            quantity,
            status,
            DateTime.UtcNow.AddMinutes(30),
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
    }

    private static StockMovement CreateMovement(
        Guid companyId,
        long warehouseId,
        long productId,
        StockMovementType type,
        int quantity,
        string operationId)
    {
        return StockMovement.Reconstitute(
            StockMovementId.From(DateTime.UtcNow.Ticks),
            CompanyId.From(companyId),
            WarehouseId.From(warehouseId),
            ProductId.From(productId),
            type,
            quantity,
            operationId,
            null,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);
    }

    private sealed class StubInventoryAccessService : IInventoryAccessService
    {
        private readonly bool _allow;

        public StubInventoryAccessService(bool allow) => _allow = allow;

        public Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, InventoryPermission permission, CancellationToken ct = default)
            => Task.FromResult(_allow);
    }

    private sealed class InMemoryWarehouseStockRepository : IWarehouseStockRepository
    {
        private readonly Dictionary<(Guid CompanyId, long WarehouseId, long ProductId), WarehouseStock> _items = new();

        public void Seed(WarehouseStock stock)
            => _items[(stock.CompanyId.Value, stock.WarehouseId.Value, stock.ProductId.Value)] = stock;

        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(WarehouseId warehouseId, ProductId productId, CancellationToken ct = default)
        {
            var match = _items.Values.FirstOrDefault(x => x.WarehouseId == warehouseId && x.ProductId == productId);
            return Task.FromResult(match);
        }

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WarehouseStock>>(_items.Values.Where(x => x.CompanyId == companyId).ToList());

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(CompanyId companyId, ProductId productId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WarehouseStock>>(_items.Values.Where(x => x.CompanyId == companyId && x.ProductId == productId).ToList());

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default)
        {
            Seed(stock);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default)
        {
            Seed(stock);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryReservationRepository : IInventoryReservationRepository
    {
        private readonly Dictionary<(Guid CompanyId, string Code), InventoryReservation> _items = new();

        public void Seed(InventoryReservation reservation)
            => _items[(reservation.CompanyId.Value, reservation.ReservationCode)] = reservation;

        public Task<InventoryReservation?> GetByCodeAsync(CompanyId companyId, string reservationCode, CancellationToken ct = default)
        {
            _items.TryGetValue((companyId.Value, reservationCode), out var reservation);
            return Task.FromResult(reservation);
        }

        public Task<IReadOnlyList<InventoryReservation>> ListExpiredActiveAsync(DateTime utcNow, CancellationToken ct = default)
        {
            var list = _items.Values
                .Where(x => x.Status == InventoryReservationStatus.Active && x.ExpiresAt <= utcNow)
                .ToList();
            return Task.FromResult<IReadOnlyList<InventoryReservation>>(list);
        }

        public Task AddAsync(InventoryReservation reservation, CancellationToken ct = default)
        {
            Seed(reservation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryReservation reservation, CancellationToken ct = default)
        {
            Seed(reservation);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryStockMovementRepository : IStockMovementRepository
    {
        public List<StockMovement> Movements { get; } = [];

        public void Seed(StockMovement movement) => Movements.Add(movement);

        public Task<bool> ExistsByOperationIdAsync(CompanyId companyId, string operationId, CancellationToken ct = default)
            => Task.FromResult(Movements.Any(x => x.CompanyId == companyId && x.OperationId == operationId));

        public Task<IReadOnlyList<StockMovement>> ListByCompanyAndProductAsync(CompanyId companyId, ProductId? productId, CancellationToken ct = default)
        {
            var result = Movements.Where(x => x.CompanyId == companyId);
            if (productId is not null)
                result = result.Where(x => x.ProductId == productId);
            return Task.FromResult<IReadOnlyList<StockMovement>>(result.ToList());
        }

        public Task AddAsync(StockMovement movement, CancellationToken ct = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult<Product?>(Product.Reconstitute(
                id,
                CompanyId.From(Guid.NewGuid()),
                $"Product {id.Value}",
                $"product-{id.Value}",
                "desc",
                new Money(11m),
                null,
                10,
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
                null));

        public Task<Product?> GetBySlugAsync(CompanyId companyId, string slug, CancellationToken ct = default)
            => Task.FromResult<Product?>(null);

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Product?>(null);

        public Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<Product>> ListPendingReviewAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task AddAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
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

    private sealed class NoOpTransactionPort : IAppTransactionPort
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
            => action(ct);
    }

    private sealed class SpyRestockNotifier : IRestockAvailabilityNotifier
    {
        public Task NotifyIfCrossedFromZeroAsync(Guid companyId, long productId, int beforeAvailableSum, int afterAvailableSum, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
