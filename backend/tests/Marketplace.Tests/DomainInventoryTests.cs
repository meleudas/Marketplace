using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Tests;

public class DomainInventoryTests
{
    [Fact]
    public void WarehouseStock_Reserve_Cannot_Exceed_Available()
    {
        var stock = WarehouseStock.Create(
            WarehouseStockId.From(1),
            CompanyId.From(Guid.NewGuid()),
            WarehouseId.From(10),
            ProductId.From(100),
            onHand: 5,
            reserved: 0,
            reorderPoint: 1);

        var ex = Assert.Throws<DomainException>(() => stock.Reserve(6));
        Assert.Contains("available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WarehouseStock_Adjust_Rejects_Reserved_Greater_Than_OnHand()
    {
        var stock = WarehouseStock.Create(
            WarehouseStockId.From(1),
            CompanyId.From(Guid.NewGuid()),
            WarehouseId.From(10),
            ProductId.From(100),
            onHand: 10,
            reserved: 1,
            reorderPoint: 1);

        Assert.Throws<DomainException>(() => stock.Adjust(2, 3, 0));
    }
}
