using Marketplace.Application.Auth.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class NotificationJobs
{
    private readonly IEmailPort _emailPort;
    private readonly ITelegramPort _telegramPort;
    private readonly ISmsPort _smsPort;

    public NotificationJobs(IEmailPort emailPort, ITelegramPort telegramPort, ISmsPort smsPort)
    {
        _emailPort = emailPort;
        _telegramPort = telegramPort;
        _smsPort = smsPort;
    }

    public Task SendConfirmationEmailAsync(string to, string token)
        => _emailPort.SendConfirmationEmailAsync(to, token);

    public Task SendPasswordResetEmailAsync(string to, string token)
        => _emailPort.SendPasswordResetEmailAsync(to, token);

    public Task SendTwoFactorEmailAsync(string to, string code)
        => _emailPort.SendTwoFactorCodeEmailAsync(to, code);

    public Task SendTelegramMessageAsync(string chatId, string message)
        => _telegramPort.SendMessageAsync(chatId, message);

    public Task SendSmsAsync(string phoneNumber, string message)
        => _smsPort.SendSmsAsync(phoneNumber, message);
}
