namespace Marketplace.Infrastructure.External.Email;

/// <summary>Інфраструктурний контракт відправки листів. Use-case шар використовує <see cref="Marketplace.Application.Auth.Ports.IEmailPort"/>.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
