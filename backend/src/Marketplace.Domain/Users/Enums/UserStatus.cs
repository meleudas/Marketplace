
namespace Marketplace.Domain.Users.Enums
{
    public enum UserStatus
    {
        Active = 1,
        PendingEmailVerify = 2,
        PendingPhoneVerify = 3,
        Suspended = 4,
        Deleted = 5
    }
}
