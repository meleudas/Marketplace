using Marketplace.Application.Companies.Authorization;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.RemoveCompanyMember;

public sealed class RemoveCompanyMemberCommandHandler : IRequestHandler<RemoveCompanyMemberCommand, Result>
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public RemoveCompanyMemberCommandHandler(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<Result> Handle(RemoveCompanyMemberCommand request, CancellationToken ct)
    {
        try
        {
            var companyId = CompanyId.From(request.CompanyId);
            if (!request.IsActorAdmin)
            {
                var actorMembership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.ActorUserId, ct);
                if (actorMembership is null || !CompanyPermissions.CanManageMembers(actorMembership.Role))
                    return Result.Failure("Forbidden");
            }

            var membership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, request.TargetUserId, ct);
            if (membership is null)
                return Result.Failure("Company member not found");

            if (membership.Role == CompanyMembershipRole.Owner)
            {
                var hasAnotherOwner = (await _companyMemberRepository.ListByCompanyAsync(companyId, ct))
                    .Any(x => x.UserId != request.TargetUserId && x.Role == CompanyMembershipRole.Owner);
                if (!hasAnotherOwner)
                    return Result.Failure("Cannot remove the last owner");
            }

            membership.SoftDelete();
            await _companyMemberRepository.UpdateAsync(membership, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Remove company member failed: {ex.Message}");
        }
    }
}
