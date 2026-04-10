using Marketplace.Application.Companies.Authorization;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetCompanyMembers;

public sealed class GetCompanyMembersQueryHandler : IRequestHandler<GetCompanyMembersQuery, Result<IReadOnlyList<CompanyMemberDto>>>
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public GetCompanyMembersQueryHandler(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<Result<IReadOnlyList<CompanyMemberDto>>> Handle(GetCompanyMembersQuery request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            if (!request.IsActorAdmin)
            {
                var actorMembership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
                if (actorMembership is null || !CompanyPermissions.CanManageMembers(actorMembership.Role))
                    return Result<IReadOnlyList<CompanyMemberDto>>.Failure("Forbidden");
            }

            var members = await _companyMemberRepository.ListByCompanyAsync(companyId, ct);
            return Result<IReadOnlyList<CompanyMemberDto>>.Success(members.Select(CompanyMemberMapper.ToDto).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CompanyMemberDto>>.Failure($"Failed to get company members: {ex.Message}");
        }
    }
}
