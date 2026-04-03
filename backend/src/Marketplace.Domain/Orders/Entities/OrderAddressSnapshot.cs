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
    public Address Address { get; private set; } = Address.Empty;

    public string FirstName => Address.FirstName;
    public string LastName => Address.LastName;
    public string Street => Address.Street;
    public string City => Address.City;
    public string State => Address.State;
    public string PostalCode => Address.PostalCode;
    public string Country => Address.Country;
    public string Phone => Address.Phone;

    public static OrderAddressSnapshot Reconstitute(
        OrderAddressId id,
        OrderId orderId,
        OrderAddressKind kind,
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
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
