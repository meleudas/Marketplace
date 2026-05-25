namespace Marketplace.Application.Notifications.Ports;

/// <summary>Marketplace user ids (same as identity id) для сповіщень адміністраторів і модераторів.</summary>
public interface IAdminNotificationRecipientIds
{
    Task<IReadOnlyList<Guid>> ListAdminUserIdsAsync(CancellationToken ct = default);
}
