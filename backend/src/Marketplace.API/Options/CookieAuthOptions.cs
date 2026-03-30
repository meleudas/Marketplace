namespace Marketplace.API.Options;

public class CookieAuthOptions
{
    public const string SectionName = "Cookies";

    public string RefreshTokenCookieName { get; set; } = "refresh_token";
    public int RefreshTokenDays { get; set; } = 30;
}
