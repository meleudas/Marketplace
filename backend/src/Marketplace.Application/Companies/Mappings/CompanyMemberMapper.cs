using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Application.Companies.Mappings;

public static class CompanyMemberMapper
{
    public static CompanyMemberDto ToDto(CompanyMember member) =>
        new(
            member.CompanyId.Value,
            member.UserId,
            member.Role.ToString(),
            member.IsOwner,
            member.CreatedAt,
            member.UpdatedAt);
}
