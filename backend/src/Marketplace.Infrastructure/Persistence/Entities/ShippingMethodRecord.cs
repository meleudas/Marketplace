namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ShippingMethodRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short Code { get; set; }
    public decimal Price { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public int EstimatedDaysMin { get; set; }
    public int EstimatedDaysMax { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
