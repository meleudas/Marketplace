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
    public Address Address { get; private set; } = Address.Empty;

    public string FirstName => Address.FirstName;
    public string LastName => Address.LastName;
    public string Street => Address.Street;
    public string City => Address.City;
    public string State => Address.State;
    public string PostalCode => Address.PostalCode;
    public string Country => Address.Country;
    public string Phone => Address.Phone;

    public static UserAddress Reconstitute(
        UserAddressId id,
        Guid userId,
        UserAddressType type,
        bool isDefault,
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
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
