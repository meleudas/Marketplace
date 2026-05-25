using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Infrastructure.Notifications;

public sealed class CompanyOrderNotificationRecipientIds : ICompanyOrderNotificationRecipientIds
{
    private readonly ICompanyMemberRepository _members;

    public CompanyOrderNotificationRecipientIds(ICompanyMemberRepository members)
    {
        _members = members;
    }

    public async Task<IReadOnlyList<Guid>> ListOwnerAndManagerUserIdsAsync(Guid companyId, CancellationToken ct = default)
    {
        var list = await _members.ListByCompanyAsync(CompanyId.From(companyId), ct);
        return list
            .Where(m => !m.IsDeleted && m.Role is CompanyMembershipRole.Owner or CompanyMembershipRole.Manager)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();
    }
}
