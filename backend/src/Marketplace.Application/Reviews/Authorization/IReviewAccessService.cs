using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Reviews.Authorization;

public interface IReviewAccessService
{
    Task<bool> HasCompanyAccessAsync(
        CompanyId companyId,
        Guid actorUserId,
        bool isActorAdmin,
        ReviewPermission permission,
        CancellationToken ct = default);
}
