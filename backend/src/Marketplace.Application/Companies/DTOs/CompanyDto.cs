namespace Marketplace.Application.Companies.DTOs;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? ImageUrl,
    string ContactEmail,
    string ContactPhone,
    CompanyAddressDto Address,
    bool IsApproved,
    DateTime? ApprovedAt,
    string? ApprovedByUserId,
    decimal? Rating,
    int ReviewCount,
    int FollowerCount,
    string? MetaRaw,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted,
    DateTime? DeletedAt);
