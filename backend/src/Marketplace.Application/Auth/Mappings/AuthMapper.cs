using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Application.Auth.Mappings;

public static class AuthMapper
{
    public static UserDto ToUserDto(User user, IReadOnlyList<UserCompanyMembershipDto>? companyMemberships = null) =>
        new(
            user.Id.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString().ToLowerInvariant(),
            user.Birthday,
            user.Avatar,
            user.IsVerified,
            user.VerificationDocument,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            user.IsDeleted,
            user.DeletedAt,
            companyMemberships ?? Array.Empty<UserCompanyMembershipDto>());

    public static AuthTokensDto ToAuthTokensDto(AuthTokens tokens) =>
        new(
            tokens.AccessToken.Value,
            tokens.RefreshToken.Token,
            tokens.AccessToken.ExpiresAt,
            tokens.RefreshToken.ExpiresAt);

    public static Email ToEmail(string email) => Email.Create(email);

    public static UserName ToUserName(string userName) => UserName.Create(userName);

    public static PhoneNumber ToPhoneNumber(string phoneNumber) => PhoneNumber.Create(phoneNumber);
}
