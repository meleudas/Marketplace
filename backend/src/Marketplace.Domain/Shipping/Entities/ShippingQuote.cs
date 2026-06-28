using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class ShippingQuote : AuditableSoftDeleteAggregateRoot<ShippingQuoteId>
{
    private ShippingQuote() { }

    public Guid UserId { get; private set; }
    public ShippingMethodId ShippingMethodId { get; private set; } = null!;
    public Money Amount { get; private set; } = Money.Zero;
    public ContactPerson Contact { get; private set; } = ContactPerson.Empty;
    public Address Address { get; private set; } = Address.Empty;
    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public static ShippingQuote Reconstitute(
        ShippingQuoteId id,
        Guid userId,
        ShippingMethodId shippingMethodId,
        Money amount,
        ContactPerson contact,
        Address address,
        DateTime expiresAtUtc,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            ShippingMethodId = shippingMethodId,
            Amount = amount,
            Contact = contact,
            Address = address,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
