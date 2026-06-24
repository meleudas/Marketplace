namespace Marketplace.Domain.Inventory.Enums;

public enum OrderFulfillmentAllocationStatus : short
{
    Reserved = 1,
    Confirmed = 2,
    Shipped = 3,
    Released = 4
}
