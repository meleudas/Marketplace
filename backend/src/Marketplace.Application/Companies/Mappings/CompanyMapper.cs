using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Application.Companies.Mappings;

public static class CompanyMapper
{
    public static CompanyDto ToDto(Company company) =>
        new(
            company.Id.Value,
            company.Name,
            company.Slug,
            company.Description,
            company.ImageUrl,
            company.ContactEmail,
            company.ContactPhone,
            ToDto(company.Address),
            company.IsApproved,
            company.ApprovedAt,
            company.ApprovedByUserId,
            company.Rating,
            company.ReviewCount,
            company.FollowerCount,
            company.Meta.Raw,
            company.CreatedAt,
            company.UpdatedAt,
            company.IsDeleted,
            company.DeletedAt);

    public static Address ToAddress(CompanyAddressDto dto) =>
        Address.Create(
            dto.Street,
            dto.City,
            dto.State,
            dto.PostalCode,
            dto.Country);

    private static CompanyAddressDto ToDto(Address address) =>
        new(
            address.Street,
            address.City,
            address.State,
            address.PostalCode,
            address.Country);
}
