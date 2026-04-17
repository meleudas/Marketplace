namespace Marketplace.Application.Auth.DTOs;

public sealed record UserCompanyMembershipDto(
    Guid CompanyId,
    string CompanyName,
    string CompanySlug,
    string MembershipRole,
    bool IsOwner);
