namespace Marketplace.Application.Auth.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int LinkCodeTtlMinutes { get; set; } = 10;
}
