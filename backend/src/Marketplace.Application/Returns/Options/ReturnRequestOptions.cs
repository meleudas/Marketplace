namespace Marketplace.Application.Returns.Options;

public sealed class ReturnRequestOptions
{
    public const string SectionName = "ReturnRequests";

    public int MaxDaysAfterDelivery { get; set; } = 14;
    public bool AllowReturnWhileShipped { get; set; } = false;
    public int ShippedReturnWindowHours { get; set; } = 48;
    public bool RequireCommentForOther { get; set; } = true;
    public bool RestockOnReceive { get; set; } = true;
}
