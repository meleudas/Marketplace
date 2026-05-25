namespace Marketplace.Application.Notifications;

public enum AppNotificationAudienceKind
{
    Admins = 0,
    User = 1,
    /// <summary>Owner/Manager members of <see cref="AppNotificationRequest.TargetCompanyId"/> (order company).</summary>
    CompanyStakeholders = 2
}
