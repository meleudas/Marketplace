using System;

namespace Marketplace.Application.Auth.DTOs
{
    public record UserDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Role,
        DateTime? Birthday,
        string? Avatar,
        bool IsVerified,
        string? VerificationDocument,
        DateTime? LastLoginAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsDeleted,
        DateTime? DeletedAt
    );
}
