namespace Marketplace.Application.Companies.DTOs;

public sealed record CompanyAddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
