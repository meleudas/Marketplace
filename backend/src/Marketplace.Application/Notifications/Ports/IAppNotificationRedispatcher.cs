namespace Marketplace.Application.Notifications.Ports;

public interface IAppNotificationRedispatcher
{
    void EnqueueDispatch(
        string templateKey,
        Guid correlationId,
        int channels,
        int audience,
        Guid? targetUserId,
        Guid? targetCompanyId,
        string? payloadJson);
}
