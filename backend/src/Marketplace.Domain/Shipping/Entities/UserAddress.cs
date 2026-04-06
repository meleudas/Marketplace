using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Domain.Shipping.Entities;

public sealed class UserAddress : AuditableSoftDeleteAggregateRoot<UserAddressId>
{
    private UserAddress() { }

    public Guid UserId { get; private set; }
    public UserAddressType Type { get; private set; }
    public bool IsDefault { get; private set; }
    public ContactPerson Contact { get; private set; } = ContactPerson.Empty;
    public Address Address { get; private set; } = Address.Empty;

    public string FirstName => Contact.FirstName;
    public string LastName => Contact.LastName;
    public string Phone => Contact.Phone;
    public string Street => Address.Street;
    public string City => Address.City;
    public string State => Address.State;
    public string PostalCode => Address.PostalCode;
    public string Country => Address.Country;

    public static UserAddress Reconstitute(
        UserAddressId id,
        Guid userId,
        UserAddressType type,
        bool isDefault,
        ContactPerson contact,
        Address address,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Type = type,
            IsDefault = isDefault,
            Contact = contact,
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
