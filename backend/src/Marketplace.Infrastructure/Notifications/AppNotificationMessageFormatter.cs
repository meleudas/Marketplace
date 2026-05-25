namespace Marketplace.Infrastructure.Notifications;

internal static class AppNotificationMessageFormatter
{
    public const int TelegramMaxLength = 4000;

    public static string BuildPlainBody(string title, string body, string? actionUrl)
    {
        var url = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
        return url is null ? $"{title}\n\n{body}" : $"{title}\n\n{body}\n\n{url}";
    }

    public static string TruncateForTelegram(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= TelegramMaxLength)
            return text;
        return text[..(TelegramMaxLength - 1)] + "…";
    }
}
