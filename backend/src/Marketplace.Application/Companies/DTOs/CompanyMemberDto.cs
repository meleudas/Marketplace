namespace Marketplace.Application.Companies.DTOs;

public sealed record CompanyMemberDto(
    Guid CompanyId,
    Guid UserId,
    string Role,
    bool IsOwner,
    DateTime CreatedAt,
    DateTime UpdatedAt);
