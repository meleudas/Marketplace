namespace Marketplace.Application.Inventory.Options;

public sealed class CheckoutInventoryOptions
{
    public const string SectionName = "CheckoutInventory";

    public int ReservationTtlMinutes { get; set; } = 30;
}
