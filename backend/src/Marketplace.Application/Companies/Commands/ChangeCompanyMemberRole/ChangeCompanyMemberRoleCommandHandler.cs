using Marketplace.Application.Companies.Authorization;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;

public sealed class ChangeCompanyMemberRoleCommandHandler : IRequestHandler<ChangeCompanyMemberRoleCommand, Result<CompanyMemberDto>>
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public ChangeCompanyMemberRoleCommandHandler(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<Result<CompanyMemberDto>> Handle(ChangeCompanyMemberRoleCommand request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            if (!request.IsActorAdmin)
            {
                var actorMembership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
                if (actorMembership is null || !CompanyPermissions.CanManageMembers(actorMembership.Role))
                    return Result<CompanyMemberDto>.Failure("Forbidden");
            }

            var membership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.TargetUserId, ct);
            if (membership is null)
                return Result<CompanyMemberDto>.Failure("Company member not found");

            if (membership.Role == CompanyMembershipRole.Owner &&
                request.Role != CompanyMembershipRole.Owner &&
                !await HasAnotherOwner(companyId, request.TargetUserId, ct))
            {
                return Result<CompanyMemberDto>.Failure("Cannot demote the last owner");
            }

            membership.ChangeRole(request.Role);
            await _companyMemberRepository.UpdateAsync(membership, ct);
            return Result<CompanyMemberDto>.Success(CompanyMemberMapper.ToDto(membership));
        }
        catch (Exception ex)
        {
            return Result<CompanyMemberDto>.Failure($"Change company role failed: {ex.Message}");
        }
    }

    private async Task<bool> HasAnotherOwner(CompanyId companyId, Guid userId, CancellationToken ct)
    {
        var members = await _companyMemberRepository.ListByCompanyAsync(companyId, ct);
        return members.Any(x => x.UserId != userId && x.Role == CompanyMembershipRole.Owner);
    }
}
