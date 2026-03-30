namespace Marketplace.Infrastructure.External.Sms;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
