namespace Marketplace.Infrastructure.External.Email;

public interface IEmailHealthProbe
{
    Task<EmailHealthStatus> CheckAsync(CancellationToken ct = default);
}

public sealed record EmailHealthStatus(bool IsHealthy, string Provider, string Message, int? StatusCode = null);

