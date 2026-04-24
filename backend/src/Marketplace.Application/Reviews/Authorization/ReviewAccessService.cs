using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Application.Reviews.Authorization;

public sealed class ReviewAccessService : IReviewAccessService
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public ReviewAccessService(ICompanyMemberRepository companyMemberRepository) =>
        _companyMemberRepository = companyMemberRepository;

    public async Task<bool> HasCompanyAccessAsync(
        CompanyId companyId,
        Guid actorUserId,
        bool isActorAdmin,
        ReviewPermission permission,
        CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        var membership = await _companyMemberRepository.GetByCompanyAndUserAsync(companyId, actorUserId, ct);
        if (membership is null)
            return false;

        return permission switch
        {
            ReviewPermission.ReplyAsCompany => membership.Role is CompanyMembershipRole.Owner or CompanyMembershipRole.Manager or CompanyMembershipRole.Seller,
            ReviewPermission.Moderate => false,
            _ => false
        };
    }
}
