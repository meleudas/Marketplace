namespace Marketplace.Domain.Inventory.Enums;

public enum StockMovementType : short
{
    Inbound = 0,
    Outbound = 1,
    Reserve = 2,
    Release = 3,
    Adjust = 4,
    TransferOut = 5,
    TransferIn = 6
}
