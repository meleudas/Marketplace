using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class ShippingMethod : AuditableSoftDeleteAggregateRoot<ShippingMethodId>
{
    private ShippingMethod() { }

    public string Name { get; private set; } = string.Empty;
    public ShippingCarrierCode Code { get; private set; }
    public Money Price { get; private set; } = Money.Zero;
    public Money? FreeShippingThreshold { get; private set; }
    public int EstimatedDaysMin { get; private set; }
    public int EstimatedDaysMax { get; private set; }
    public bool IsActive { get; private set; }

    public static ShippingMethod Reconstitute(
        ShippingMethodId id,
        string name,
        ShippingCarrierCode code,
        Money price,
        Money? freeShippingThreshold,
        int estimatedDaysMin,
        int estimatedDaysMax,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Code = code,
            Price = price,
            FreeShippingThreshold = freeShippingThreshold,
            EstimatedDaysMin = estimatedDaysMin,
            EstimatedDaysMax = estimatedDaysMax,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
