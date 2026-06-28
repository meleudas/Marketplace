namespace Marketplace.Application.Orders.Options;

public sealed class OrderCancellationOptions
{
    public const string SectionName = "OrderCancellation";

    public int BuyerPendingWindowMinutes { get; set; } = 60;
    public int BuyerPaidWindowHours { get; set; } = 24;
    public int SellerProcessingWindowHours { get; set; } = 72;
    public bool RequireCommentForOther { get; set; } = true;
}
