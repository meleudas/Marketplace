using System.Net.Http.Json;
using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Telegram;

public sealed class TelegramBotSender : ITelegramPort
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramBotSender> _logger;

    public TelegramBotSender(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramBotSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendMessageAsync(string chatId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            throw new InvalidOperationException("Telegram bot token is not configured.");

        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
        var payload = new { chat_id = chatId, text = message };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Telegram API sendMessage failed. Status: {StatusCode}. Body: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException("Telegram message delivery failed.");
        }
    }
}
