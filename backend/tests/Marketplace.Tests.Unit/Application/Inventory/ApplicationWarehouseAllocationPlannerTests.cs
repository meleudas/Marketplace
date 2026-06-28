using Marketplace.Application.Inventory.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Tests.Unit.Application.Inventory;

[Trait("Suite", "Inventory")]
public sealed class ApplicationWarehouseAllocationPlannerTests
{
    private static readonly CompanyId CompanyId = CompanyId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly ProductId ProductId = ProductId.From(42);

    [Fact]
    public async Task PlanAsync_UsesHigherPriorityWarehouseFirst()
    {
        var whLow = Warehouse.Reconstitute(WarehouseId.From(1), CompanyId, "Low", "LOW", Address.Empty, "UTC", 1, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var whHigh = Warehouse.Reconstitute(WarehouseId.From(2), CompanyId, "High", "HIGH", Address.Empty, "UTC", 10, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var planner = CreatePlanner(
            [whLow, whHigh],
            [
                Stock(whLow.Id, 5),
                Stock(whHigh.Id, 5)
            ]);

        var plan = await planner.PlanAsync(CompanyId, [new WarehouseAllocationLineRequest(ProductId, 3)]);

        Assert.True(plan.IsValid);
        Assert.Single(plan.Lines);
        Assert.Equal(2, plan.Lines[0].WarehouseId.Value);
        Assert.Equal(3, plan.Lines[0].Quantity);
    }

    [Fact]
    public async Task PlanAsync_SplitsQuantityAcrossWarehouses()
    {
        var wh1 = Warehouse.Reconstitute(WarehouseId.From(1), CompanyId, "WH1", "WH1", Address.Empty, "UTC", 10, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var wh2 = Warehouse.Reconstitute(WarehouseId.From(2), CompanyId, "WH2", "WH2", Address.Empty, "UTC", 5, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var planner = CreatePlanner(
            [wh1, wh2],
            [
                Stock(wh1.Id, 3),
                Stock(wh2.Id, 4)
            ]);

        var plan = await planner.PlanAsync(CompanyId, [new WarehouseAllocationLineRequest(ProductId, 5)]);

        Assert.True(plan.IsValid);
        Assert.Equal(2, plan.Lines.Count);
        Assert.Equal(3, plan.Lines.Single(x => x.WarehouseId.Value == 1).Quantity);
        Assert.Equal(2, plan.Lines.Single(x => x.WarehouseId.Value == 2).Quantity);
    }

    [Fact]
    public async Task PlanAsync_Fails_WhenSingleWarehouseInsufficientButAggregateWouldNotHelp()
    {
        var wh1 = Warehouse.Reconstitute(WarehouseId.From(1), CompanyId, "WH1", "WH1", Address.Empty, "UTC", 10, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var planner = CreatePlanner([wh1], [Stock(wh1.Id, 2)]);

        var plan = await planner.PlanAsync(CompanyId, [new WarehouseAllocationLineRequest(ProductId, 5)]);

        Assert.False(plan.IsValid);
        Assert.Contains("Insufficient stock", plan.ErrorMessage);
    }

    [Fact]
    public async Task PlanAsync_Succeeds_WhenAggregateStockCoversQuantity()
    {
        var wh1 = Warehouse.Reconstitute(WarehouseId.From(1), CompanyId, "WH1", "WH1", Address.Empty, "UTC", 10, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var wh2 = Warehouse.Reconstitute(WarehouseId.From(2), CompanyId, "WH2", "WH2", Address.Empty, "UTC", 5, true, DateTime.UtcNow, DateTime.UtcNow, false, null);
        var planner = CreatePlanner(
            [wh1, wh2],
            [
                Stock(wh1.Id, 2),
                Stock(wh2.Id, 4)
            ]);

        var plan = await planner.PlanAsync(CompanyId, [new WarehouseAllocationLineRequest(ProductId, 5)]);

        Assert.True(plan.IsValid);
        Assert.Equal(5, plan.Lines.Sum(x => x.Quantity));
    }

    private static WarehouseAllocationPlanner CreatePlanner(
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<WarehouseStock> stocks) =>
        new(new FakeWarehouseRepository(warehouses), new FakeWarehouseStockRepository(stocks));

    private static WarehouseStock Stock(WarehouseId warehouseId, int available) =>
        WarehouseStock.Reconstitute(
            WarehouseStockId.From(warehouseId.Value * 100),
            CompanyId,
            warehouseId,
            ProductId,
            available,
            0,
            0,
            1,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private sealed class FakeWarehouseRepository(IReadOnlyList<Warehouse> warehouses) : IWarehouseRepository
    {
        public Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default) =>
            Task.FromResult(warehouses.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Warehouse>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Warehouse>>(warehouses.Where(x => x.CompanyId == companyId).ToList());

        public Task AddAsync(Warehouse warehouse, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWarehouseStockRepository(IReadOnlyList<WarehouseStock> stocks) : IWarehouseStockRepository
    {
        public Task<WarehouseStock?> GetByWarehouseAndProductAsync(
            WarehouseId warehouseId,
            ProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult(stocks.FirstOrDefault(x => x.WarehouseId == warehouseId && x.ProductId == productId));

        public Task<IReadOnlyList<WarehouseStock>> ListByProductAsync(
            CompanyId companyId,
            ProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>(stocks.Where(x => x.ProductId == productId).ToList());

        public Task<IReadOnlyList<WarehouseStock>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WarehouseStock>>(stocks.Where(x => x.CompanyId == companyId).ToList());

        public Task AddAsync(WarehouseStock stock, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(WarehouseStock stock, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
