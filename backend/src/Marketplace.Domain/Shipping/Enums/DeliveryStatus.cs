namespace Marketplace.Domain.Shipping.Enums;

public enum DeliveryStatus : short
{
    Created = 0,
    LabelGenerated = 1,
    InTransit = 2,
    Delivered = 3,
    Failed = 4,
    Returned = 5
}
