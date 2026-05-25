namespace Marketplace.Application.Notifications.Ports;

/// <summary>Marketplace user ids that should receive company-scoped order notifications (Owner/Manager).</summary>
public interface ICompanyOrderNotificationRecipientIds
{
    Task<IReadOnlyList<Guid>> ListOwnerAndManagerUserIdsAsync(Guid companyId, CancellationToken ct = default);
}
