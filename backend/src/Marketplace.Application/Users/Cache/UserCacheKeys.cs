namespace Marketplace.Application.Users.Cache;

public static class UserCacheKeys
{
    public const string ProfilePrefix = "users:profile:";
    public const string All = "users:list:all";
    public const string SearchByUserNamePrefix = "users:search:username:";

    public static string Profile(Guid identityUserId) => $"{ProfilePrefix}{identityUserId}";

    public static string SearchByUserName(string userName)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        return $"{SearchByUserNamePrefix}{normalized}";
    }
}
