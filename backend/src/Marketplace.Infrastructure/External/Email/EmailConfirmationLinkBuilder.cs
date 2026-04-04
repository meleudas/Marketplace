namespace Marketplace.Infrastructure.External.Email;

internal static class EmailConfirmationLinkBuilder
{
    /// <summary>Будує URL сторінки підтвердження на фронті: /confirm-email?email=...&token=...</summary>
    public static string Build(string? frontendBaseUrl, string email, string token)
    {
        var baseUrl = (frontendBaseUrl ?? "").Trim().TrimEnd('/');
        if (baseUrl.Length == 0)
            baseUrl = "http://localhost:3000";

        return $"{baseUrl}/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}
