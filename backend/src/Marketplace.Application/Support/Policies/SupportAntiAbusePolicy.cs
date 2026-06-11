using Marketplace.Application.Support.Options;
using Marketplace.Domain.Support.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Policies;

public sealed class SupportAntiAbusePolicy
{
    private readonly ISupportTicketRepository _tickets;
    private readonly SupportOptions _options;

    public SupportAntiAbusePolicy(ISupportTicketRepository tickets, IOptions<SupportOptions> options)
    {
        _tickets = tickets;
        _options = options.Value;
    }

    public async Task<(bool Allowed, string? Reason)> EvaluateCreateAsync(string userId, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-Math.Max(1, _options.CreateRateLimitWindowMinutes));
        var count = await _tickets.CountRecentByUserAsync(userId, since, ct);
        if (count >= Math.Max(1, _options.CreateRateLimitPerWindow))
            return (false, "rate exceeded: support ticket create limit");
        return (true, null);
    }
}
