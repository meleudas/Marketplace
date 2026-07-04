using System;

namespace Marketplace.Application.Auth.DTOs
{
    public record UserDto(
        Guid Id,
        string FirstName,
        string LastName,
        string? Patronymic,
        string Role,
        string? Email,
        string? PhoneNumber,
        DateTime? Birthday,
        string? Avatar,
        bool IsVerified,
        string? VerificationDocument,
        DateTime? LastLoginAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsDeleted,
        DateTime? DeletedAt,
        IReadOnlyList<UserCompanyMembershipDto> CompanyMemberships
    );
}
