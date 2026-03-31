namespace Marketplace.Infrastructure.External.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public int LinkCodeTtlMinutes { get; set; } = 10;
}
