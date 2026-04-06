namespace Marketplace.Domain.Common.ValueObjects;

/// <summary>
/// Контактні дані отримувача/контактної особи (окремо від адреси).
/// </summary>
public sealed record ContactPerson
{
    public static ContactPerson Empty { get; } = new();

    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;

    public static ContactPerson Create(
        string firstName,
        string lastName,
        string phone) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone
        };
}
