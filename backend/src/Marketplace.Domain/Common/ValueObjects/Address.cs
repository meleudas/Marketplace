namespace Marketplace.Domain.Common.ValueObjects;

/// <summary>
/// Універсальна адреса для повторного використання в різних модулях домену.
/// </summary>
public sealed record Address
{
    public static Address Empty { get; } = new();

    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;

    public static Address Create(
        string firstName,
        string lastName,
        string street,
        string city,
        string state,
        string postalCode,
        string country,
        string phone) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Street = street,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country,
            Phone = phone
        };
}
