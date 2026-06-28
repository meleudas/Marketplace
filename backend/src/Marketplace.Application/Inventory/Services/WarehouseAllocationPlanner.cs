using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Application.Inventory.Services;

public sealed record WarehouseAllocationLineRequest(ProductId ProductId, int Quantity);

public sealed record WarehouseAllocationPlanLine(
    ProductId ProductId,
    WarehouseId WarehouseId,
    int Quantity);

public sealed record WarehouseAllocationPlan(
    bool IsValid,
    string? ErrorMessage,
    IReadOnlyList<WarehouseAllocationPlanLine> Lines)
{
    public static WarehouseAllocationPlan Invalid(string message) => new(false, message, []);

    public static WarehouseAllocationPlan Valid(IReadOnlyList<WarehouseAllocationPlanLine> lines) => new(true, null, lines);
}

public sealed class WarehouseAllocationPlanner
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseStockRepository _stockRepository;

    public WarehouseAllocationPlanner(
        IWarehouseRepository warehouseRepository,
        IWarehouseStockRepository stockRepository)
    {
        _warehouseRepository = warehouseRepository;
        _stockRepository = stockRepository;
    }

    public async Task<WarehouseAllocationPlan> PlanAsync(
        CompanyId companyId,
        IReadOnlyList<WarehouseAllocationLineRequest> lines,
        CancellationToken ct = default)
    {
        if (lines.Count == 0)
            return WarehouseAllocationPlan.Valid([]);

        var warehouses = (await _warehouseRepository.ListByCompanyAsync(companyId, ct))
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id.Value)
            .ToList();

        if (warehouses.Count == 0)
            return WarehouseAllocationPlan.Invalid("No active warehouses");

        var warehousePriority = warehouses.Select(x => x.Id).ToList();
        var stockByProduct = new Dictionary<long, List<WarehouseStock>>();

        foreach (var line in lines)
        {
            if (!stockByProduct.ContainsKey(line.ProductId.Value))
            {
                var stocks = await _stockRepository.ListByProductAsync(companyId, line.ProductId, ct);
                stockByProduct[line.ProductId.Value] = stocks.ToList();
            }
        }

        var planLines = new List<WarehouseAllocationPlanLine>();

        foreach (var line in lines)
        {
            var remaining = line.Quantity;
            var stocks = stockByProduct[line.ProductId.Value];
            var stockByWarehouse = stocks.ToDictionary(x => x.WarehouseId.Value);

            foreach (var warehouseId in warehousePriority)
            {
                if (remaining <= 0)
                    break;

                if (!stockByWarehouse.TryGetValue(warehouseId.Value, out var stock))
                    continue;

                var take = Math.Min(remaining, stock.Available);
                if (take <= 0)
                    continue;

                planLines.Add(new WarehouseAllocationPlanLine(line.ProductId, warehouseId, take));
                remaining -= take;
            }

            if (remaining > 0)
                return WarehouseAllocationPlan.Invalid($"Insufficient stock for product {line.ProductId.Value}");
        }

        return WarehouseAllocationPlan.Valid(planLines);
    }
}
