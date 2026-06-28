using System.Security.Cryptography;
using System.Text;
using Marketplace.Application.Support.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Policies;

public sealed class HelpdeskWebhookSignatureValidator
{
    private readonly SupportOptions _options;

    public HelpdeskWebhookSignatureValidator(IOptions<SupportOptions> options) => _options = options.Value;

    public bool IsValid(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSigningSecret))
            return true;
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var key = Encoding.UTF8.GetBytes(_options.WebhookSigningSecret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        var provided = signatureHeader.Trim().Replace("sha256=", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
    }
}
