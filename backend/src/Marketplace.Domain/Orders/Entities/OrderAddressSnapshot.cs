using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Domain.Orders.Entities;

/// <summary>Незмінний snapshot адреси для замовлення (order_addresses).</summary>
public sealed class OrderAddressSnapshot : AuditableSoftDeleteAggregateRoot<OrderAddressId>
{
    private OrderAddressSnapshot() { }

    public OrderId OrderId { get; private set; } = null!;
    public OrderAddressKind Kind { get; private set; }
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

    public static OrderAddressSnapshot Reconstitute(
        OrderAddressId id,
        OrderId orderId,
        OrderAddressKind kind,
        ContactPerson contact,
        Address address,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            Kind = kind,
            Contact = contact,
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
