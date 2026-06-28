namespace Marketplace.Application.Notifications;

[Flags]
public enum PushSubscriptionAudienceFlags
{
    None = 0,
    UserWebPush = 1,
    AdminWebPush = 2
}
