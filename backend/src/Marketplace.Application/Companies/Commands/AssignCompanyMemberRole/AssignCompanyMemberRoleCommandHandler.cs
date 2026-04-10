using Marketplace.Application.Companies.Authorization;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;

public sealed class AssignCompanyMemberRoleCommandHandler : IRequestHandler<AssignCompanyMemberRoleCommand, Result<CompanyMemberDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyMemberRepository _companyMemberRepository;
    private readonly IUserRepository _userRepository;

    public AssignCompanyMemberRoleCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyMemberRepository companyMemberRepository,
        IUserRepository userRepository)
    {
        _companyRepository = companyRepository;
        _companyMemberRepository = companyMemberRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CompanyMemberDto>> Handle(AssignCompanyMemberRoleCommand request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            var company = await _companyRepository.GetByIdAsync(companyId, ct);
            if (company is null)
                return Result<CompanyMemberDto>.Failure("Company not found");

            var user = await _userRepository.GetByIdentityIdAsync(Domain.Users.ValueObjects.IdentityUserId.From(request.TargetUserId), ct);
            if (user is null)
                return Result<CompanyMemberDto>.Failure("User not found");

            if (!request.IsActorAdmin)
            {
                var actorMembership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
                if (actorMembership is null || !CompanyPermissions.CanManageMembers(actorMembership.Role))
                    return Result<CompanyMemberDto>.Failure("Forbidden");
            }

            var membership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.TargetUserId, ct);
            if (membership is null)
            {
                membership = CompanyMember.Create(companyId, request.TargetUserId, request.Role);
                await _companyMemberRepository.AddAsync(membership, ct);
            }
            else
            {
                membership.ChangeRole(request.Role);
                await _companyMemberRepository.UpdateAsync(membership, ct);
            }

            return Result<CompanyMemberDto>.Success(CompanyMemberMapper.ToDto(membership));
        }
        catch (Exception ex)
        {
            return Result<CompanyMemberDto>.Failure($"Assign company role failed: {ex.Message}");
        }
    }
}
